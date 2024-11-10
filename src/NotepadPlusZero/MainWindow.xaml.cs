using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NotepadPlusZero.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Storage.Pickers;
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

        private async void OpenFile()
        {
            var filePicker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
            filePicker.FileTypeFilter.Add("*");
            var file = await filePicker.PickSingleFileAsync();
            if (file is null) return;
            FilePath = file.Path;

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

        private async void UpdateTitle()
        {
            if (FilePath is null) Title = "Notepad+0";

            EditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string bufferContent);
            string fileContent;
            try
            {
                fileContent = await File.ReadAllTextAsync(FilePath);
            }
            catch (Exception ex)
            {
                return;
            }

            if (fileContent == bufferContent)
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
    }
}
