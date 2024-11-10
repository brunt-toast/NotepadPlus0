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

        public MainWindow()
        {
            OpenFileCommand = new Command(OpenFile, () => true);
            SaveFileCommand = new Command(SaveFile, () => true);
            SaveFileAsCommand = new Command(SaveFileAs, () => true);

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
            catch
            {
                throw new NotImplementedException("Should be a dialog here");
            }
        }

        private async void SaveFile()
        {
            if (FilePath is null)
            {
                SaveFileAs();
                return;
            }

            EditBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string textboxContent);
            await File.WriteAllTextAsync(FilePath, textboxContent);
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
