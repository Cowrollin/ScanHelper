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
using static ScanHelper.Pixel;

namespace ScanHelper.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private Bitmap? _ScanImage;
    [ObservableProperty] 
    private WriteableBitmap _bitmapImage;
    private WriteableBitmap wrtBitmap;
    private byte WhiteSensivity = 190;
    private byte BufferWhiteZone = 1;
    
    public event EventHandler MyEvent;


    
    public MainWindowViewModel()
    {
        wrtBitmap = new WriteableBitmap(new PixelSize(100, 100), new Vector(100, 100), PixelFormat.Bgra8888);
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

    public void SaveButtonClicked()
    {
        BitmapImage.Save("testimage.jpg");
    }

    private unsafe void RedRectangle()
    {
        int w = wrtBitmap.PixelSize.Width;
        int h = wrtBitmap.PixelSize.Height;
        int minX = w, minY = h, maxX = 0, maxY = 0;
        using (var buf = wrtBitmap.Lock())
        {
            var ptr = (uint*)buf.Address;
            int c = 1;
            bool inPhoto = false;
            int whiteRowCount = 0;
            for (int y = 0; y < h-1; y++)
            {
                bool nonWhiteZone = false;
                for (int x = 0; x < w; x++)
                {
                    Pixel pxl = new Pixel(ptr[c]);
                    if ((pxl.GetPixelArgb(ColorChannel.R) < WhiteSensivity) || (pxl.GetPixelArgb(ColorChannel.G) < WhiteSensivity) || (pxl.GetPixelArgb(ColorChannel.B) < WhiteSensivity))
                    {
                        nonWhiteZone = true;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                    c++;
                }
                if (nonWhiteZone)
                {
                    whiteRowCount = 0;
                    inPhoto = true;
                }
                else if (inPhoto)
                {
                    whiteRowCount++;
                    if (whiteRowCount >= BufferWhiteZone)
                    {
                        Console.WriteLine($"Photo!");
                        DrawRectangle(minX, minY, maxX, maxY);
                        minX = w;
                        minY = h;
                        maxX = 0; 
                        maxY = 0;
                        inPhoto = false;
                        whiteRowCount = 0;
                    }
                }
            }

            if (minX < maxX && minY < maxY)
            {
                DrawRectangle(minX, minY, maxX, maxY);
            }
        }
    }

    private unsafe void DrawRectangle(int _x, int _y, int _xx, int _yy)
    {
        using (var buf = wrtBitmap.Lock())
        {
            int h = _yy - _y;
            int w = _xx - _x;
            int c = 0;
            var ptr = (uint*)buf.Address;
            for (int y = 0; y < wrtBitmap.Size.Height; y++)
            {
                for (int x = 0; x < wrtBitmap.Size.Width; x++)
                {
                    if (y == _y && x >= _x && x <= _xx)  
                        *(ptr + c) = 0xFFFF0000;  // верхняя граница
                    if (y == _yy && x >= _x && x <= _xx)  
                        *(ptr + c) = 0xFFFF0000;  // нижняя граница
                    if (x == _x && y >= _y && y <= _yy)  
                        *(ptr + c) = 0xFFFF0000;  // левая граница
                    if (x == _xx && y >= _y && y <= _yy)  
                        *(ptr + c) = 0xFFFF0000;  // правая граница
                    c++;
                }
            }

        }
    }

    // -------------------------------
    public unsafe void ResetBitmap()
    {
        using (var buf = wrtBitmap.Lock())
        {
            var ptr = (uint*)buf.Address;

            var w = wrtBitmap.PixelSize.Width;
            var h = wrtBitmap.PixelSize.Height;
            
            // Clear.
            for (var i = 0; i < w * (h - 1); i++)
            {
                *(ptr + i) = 0xFFFFFFFF;
            }
        }
    }

    private void Update()
    {
        BitmapImage = wrtBitmap;
        TriggerMyEvent();
    }
}