using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NotepadPlusZero.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Pickers.Provider;
using static System.Net.Mime.MediaTypeNames;

namespace NotepadPlusZero
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const double DefaultFontSize = 14;
        private string? _filePath = null;

        public string? FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand SaveFileAsCommand { get; }
        public ICommand ShowNotImplementedMessageCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand RestoreDefaultZoomCommand { get; }

        public MainWindow()
        {
            OpenFileCommand = new Command(OpenFile, () => true);
            SaveFileCommand = new Command(SaveFile, () => true);
            SaveFileAsCommand = new Command(SaveFileAs, () => true);
            ShowNotImplementedMessageCommand = new Command(() => NotImplementedTeachingTip.IsOpen = true, () => true);
            ZoomInCommand = new Command(() => EditBox.FontSize += 1, () => true);
            ZoomOutCommand = new Command(() => EditBox.FontSize -= 1, () => true);
            RestoreDefaultZoomCommand = new Command(() => EditBox.FontSize = DefaultFontSize, () => true);

            InitializeComponent();

            Title = "Notepad+0";
        }

        /// <summary>
        ///     Prompt the user to select a file, then load it into the buffer. 
        ///     If the user cancels, do nothing. 
        /// </summary>
        private async void OpenFile()
        {
            var filePicker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
            filePicker.FileTypeFilter.Add("*");
            var file = await filePicker.PickSingleFileAsync();
            if (file is null) return;
            if (!await ConfirmDiscardBuffer()) return;
            
            FilePath = file.Path;

            await LoadFileFromFilePath();
        }

        private async Task<bool> ConfirmDiscardBuffer()
        {
            if (FilePath is null || await FileHasChanges())
            {
                bool cancel = false;
                ContentDialog dialog = new ContentDialog()
                {
                    XamlRoot = EditBox.XamlRoot,
                    Title = "Unsaved changes",
                    Content = "The current file has unsaved changes. Are you sure you want to continue?",
                    PrimaryButtonText = "Save and continue",
                    SecondaryButtonText = "Continue without saving",
                    CloseButtonText = "Cancel",
                    PrimaryButtonCommand = new Command(SaveFile, () => true),
                    SecondaryButtonCommand = new Command(() => { }, () => true),
                    CloseButtonCommand = new Command(() => cancel = true, () => true),
                    DefaultButton = ContentDialogButton.Close
                };
                await dialog.ShowAsync();
                return !cancel;
            }

            return true;
        }

        private async Task LoadFileFromFilePath()
        {
            try
            {
                string content = await File.ReadAllTextAsync(FilePath);
                EditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, content);
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = EditBox.XamlRoot,
                    Title = "Something went wrong",
                    Content = $"An error occured while reading the file. ({ex.Message})"
                };
                await dialog.ShowAsync();
            }

            UpdateTitle();
        }

        /// <summary>
        ///     Save the buffer to the selected file path. 
        ///     If there is no path, call <see cref="SaveFileAs"/>. 
        /// </summary>
        private async void SaveFile()
        {
            if (FilePath is null)
            {
                SaveFileAs();
                return;
            }

            EditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string textboxContent);
            try
            {
                await File.WriteAllTextAsync(FilePath, textboxContent);
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = EditBox.XamlRoot,
                    Title = "Something went wrong",
                    Content = $"An error occured while writing the file. ({ex.Message})"
                };
                await dialog.ShowAsync();
            }
            UpdateTitle();
        }

        /// <summary>
        ///     Prompt the user to choose a new file location, then save to that location.
        /// </summary>
        private async void SaveFileAs()
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = "New Document";
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            var file = await savePicker.PickSaveFileAsync();
            if (file is null) return;
            FilePath = file.Path;
            SaveFile();
        }

        /// <summary>
        ///     Update the window title with the file name and save state. 
        /// </summary>
        private async void UpdateTitle()
        {
            if (FilePath is null) Title = "Notepad+0";

            if (await FileHasChanges())
            {
                Title = $"Notepad+0 - {FilePath}";
            }
            else
            {
                Title = $"Notepad+0 - {FilePath}*";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void EditBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private async void EditBox_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
                if (items is null) return;
                if (items.Count != 1) return;
                if (items[0] is not StorageFile file) return;
                if (!await ConfirmDiscardBuffer()) return;
                FilePath = file.Path;
                await LoadFileFromFilePath();
            }
        }

        private async Task<bool> FileHasChanges()
        {
            EditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string contentInBuffer);
            if (FilePath is null) return !string.IsNullOrEmpty(contentInBuffer);

            string fileOnDisk = await File.ReadAllTextAsync(FilePath);
            return fileOnDisk == contentInBuffer;
        }
    }
}
