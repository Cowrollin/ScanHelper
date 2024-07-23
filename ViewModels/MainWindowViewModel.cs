using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using ScanHelper.Views;

namespace ScanHelper.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private Bitmap? _ScanImage;
    [ObservableProperty] 
    private WriteableBitmap _bitmapImage;
    private WriteableBitmap wrtBitmap;
    public event EventHandler MyEvent;


    
    public MainWindowViewModel()
    {
        wrtBitmap = new WriteableBitmap(new PixelSize(100, 100), new Vector(100, 100), PixelFormat.Bgra8888);
        ResetBitmap(0);
        Update();
    }
    
    public void TriggerMyEvent()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
    
    // AddImage 
    public async void AddButtonCliked()
    {
        // выбираем файл с изображением
        var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "Изображения", Extensions = { "png", "jpg", "jpeg", "bmp" } }
            }
        };
        
        // путь до файла изображения
        var result = await dialog.ShowAsync(new Window());
        if (result != null && result.Length > 0)
        {
            string selectedFile = result[0];
            if (File.Exists(selectedFile))
            {
                _ScanImage = new Bitmap(selectedFile);
                wrtBitmap = new WriteableBitmap(_ScanImage.PixelSize, _ScanImage.Dpi, PixelFormat.Bgra8888);
                using (var lockbmp = wrtBitmap.Lock())
                {
                    _ScanImage.CopyPixels(new PixelRect(0,0, _ScanImage.PixelSize.Width, _ScanImage.PixelSize.Height), lockbmp.Address, lockbmp.RowBytes * lockbmp.Size.Height, lockbmp.RowBytes);
                }
            }
        }
        Update();
    }

    public void SetButtonClicked()
    {
        RedRectangle();
        Update();
    }

    private unsafe void RedRectangle()
    {
        using (var buf = wrtBitmap.Lock())
        {
            int w = wrtBitmap.PixelSize.Width;
            int h = wrtBitmap.PixelSize.Height;
            var ptr = (uint*)buf.Address;
            Console.WriteLine($"index {(int)ptr}");
            for (var i = 0; i < w * (h - 1); i++)
            {
                uint pixel = ptr[i];
                
                // Разлагаем значение пикселя на компоненты ARGB
                byte A = (byte)((pixel >> 24) & 0xFF);
                byte R = (byte)((pixel >> 16) & 0xFF);
                byte G = (byte)((pixel >> 8) & 0xFF);
                byte B = (byte)(pixel & 0xFF);

                if (!(A == 255 && R == 255 && G == 255 && B == 255))
                {
                    *(ptr + i) = 0xFF000000;
                }
            }
        }
    } 
    
    // -------------------------------
    public unsafe void ResetBitmap(int q)
    {
        using (var buf = wrtBitmap.Lock())
        {
            var ptr = (uint*)buf.Address;

            var w = wrtBitmap.PixelSize.Width;
            var h = wrtBitmap.PixelSize.Height;
            
            // Clear.
            for (var i = 0; i < w * (h - 1); i++)
            {
                if (i % 2 == q)
                {
                    *(ptr + i) = 0xFFFF0000;
                }
            }
        }
    }

    private void Update()
    {
        BitmapImage = wrtBitmap;
        TriggerMyEvent();
    }
}