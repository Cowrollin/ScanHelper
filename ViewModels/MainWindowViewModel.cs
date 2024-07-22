using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace ScanHelper.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private Bitmap? _ScanImage;
    public Bitmap? ScanImage
    {
        get => _ScanImage; 
        private set => this.RaiseAndSetIfChanged(ref _ScanImage, value);
    }

    public MainWindowViewModel()
    {
        
    }

    [Obsolete("Obsolete")]
    public async void AddButtonCliked()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "Изображения", Extensions = { "png", "jpg", "jpeg", "bmp" } }
            }
        };

        var result = await dialog.ShowAsync(new Window());
        if (result != null && result.Length > 0)
        {
            string selectedFile = result[0];
            if (File.Exists(selectedFile))
            {
                ScanImage = new Bitmap(selectedFile);
            }
        }
    }
    
    
}