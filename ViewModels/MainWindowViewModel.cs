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
    
    
    public MainWindowViewModel()
    {
        wrtBitmap = new WriteableBitmap(new PixelSize(100, 100), new Vector(100, 100), PixelFormat.Bgra8888);
        ResetBitmap(0);
        Update();
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
                BitmapImage = new WriteableBitmap(_ScanImage.PixelSize, _ScanImage.Dpi, PixelFormat.Bgra8888);
                using (var lockbmp = BitmapImage.Lock())
                {
                    _ScanImage.CopyPixels(new PixelRect(0,0, _ScanImage.PixelSize.Width, _ScanImage.PixelSize.Height), lockbmp.Address, lockbmp.RowBytes * lockbmp.Size.Height, lockbmp.RowBytes);
                }
            }
        }
    }

    public void SetButtonClicked()
    {
        ResetBitmap(1);
        Update();
    }

    private unsafe void RedRectangle()
    {
        using (var buf = BitmapImage.Lock())
        {
            int w = BitmapImage.PixelSize.Width;
            int h = BitmapImage.PixelSize.Height;
            var ptr = (uint*)buf.Address;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    *(ptr + x + y) = 0xFFFFFFFF;
                }
            }
        }
        

        /*
        using (var buf = _BitmapImage.Lock())
        {
            int w = _BitmapImage.PixelSize.Width;
            int h = _BitmapImage.PixelSize.Height;
            int stride = buf.RowBytes;
            IntPtr buffer = buf.Address;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    IntPtr pixelPtr = buffer + y * stride + x * 4;
                    int pixel = Marshal.ReadInt32(pixelPtr);

                    byte blue = (byte)(pixel & 0xFF);
                    byte green = (byte)((pixel >> 8) & 0xFF);
                    byte red = (byte)((pixel >> 16) & 0xFF);
                    byte alpha = (byte)((pixel >> 24) & 0xFF);

                    if (!(red == 255 && green == 255 && blue == 255))
                    {
                        red = 255;
                        green = 0;
                        blue = 0;

                        int newPixel = (alpha << 24) | (red << 16) | (green << 8) | blue;
                        Marshal.WriteInt32(pixelPtr, newPixel);
                        Console.WriteLine("Paint!");
                    }
                }
            }
        }*/

        
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
            Console.WriteLine($"Done!");
        }
    }

    private void Update()
    {
        BitmapImage = wrtBitmap;
    }
}