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
    private byte WhiteSensivity = 240;
    private byte BufferWhiteZone = 2;
    private int MinWidthPhoto = 600;
    private int MinHeightPhoto = 600;
    private List<RectCoord> PhotoMass = new List<RectCoord> {};

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
    
    
    
    // Выделяем
    private unsafe void RedRectangle()
    {
        List<RectCoord> imageBounds = FindAllImageBounds();
        int c = 0;
        foreach (var item in imageBounds)
        {
            int x, y, xx, yy;
            (x, y, xx, yy) = item.GetCoord();
            if ((xx - x > MinWidthPhoto) && (yy - y) > MinHeightPhoto)
            {
                c++;
                Console.WriteLine($" - {item.GetCoord()}");
            }
        }
        Console.WriteLine(c);
        /*GetCoord(true);
        foreach (var item in PhotoMass)
        {
            Console.WriteLine($"{item.GetCoord()}");
        }
        GetCoord(false);
        Console.WriteLine("");
        foreach (var item in PhotoMass)
        {
            Console.WriteLine($"{item.GetCoord()}");
        }*/
    }
    
    // метод для определения границ с буфером по ширине и высоте
    private unsafe void GetCoord(bool _axis)
    {
        int w = wrtBitmap.PixelSize.Width;
        int h = wrtBitmap.PixelSize.Height;
        int minX = w, minY = h, maxX = 0, maxY = 0;
        
        if (_axis) // y --------------------------------------
        {
            using (var buf = wrtBitmap.Lock())
            {
                var ptr = (uint*)buf.Address;
                int c = 0;
                bool inPhoto = false;
                int whiteRowCount = 0;
            
                for (int y = 0; y < h; y++)
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
                            RectCoord secRectCoord = new RectCoord(minX, minY, maxX, maxY);
                            PhotoMass.Add(secRectCoord);
                            
                            minX = w;
                            minY = h;
                            maxX = 0; 
                            maxY = 0;
                            inPhoto = false;
                            whiteRowCount = 0;
                        }
                    }
                }
            }
        }
        else // x ------------------------
        {
            using (var buf = wrtBitmap.Lock())
            {
                var ptr = (uint*)buf.Address;
                int c = 1;
                bool inPhoto = false;
                int whiteCollumnCount = 0;
                
                for (int x = 0; x < w; x++)
                {
                    bool nonWhiteZone = false;
                    for (int y = 0; y < h; y++)
                    {
                        Pixel pxl = new Pixel(ptr[y * w + x]);
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
                        whiteCollumnCount = 0;
                        inPhoto = true;
                    }
                    else if (inPhoto)
                    {
                        whiteCollumnCount++;
                        if (whiteCollumnCount >= BufferWhiteZone)
                        {
                            RectCoord secRectCoord = new RectCoord(minX, minY, maxX, maxY);
                            PhotoMass.Add(secRectCoord);
                            
                            minX = w;
                            minY = h;
                            maxX = 0; 
                            maxY = 0;
                            inPhoto = false;
                            whiteCollumnCount = 0;
                        }
                    }
                }
            }
        }
    }

    private unsafe List<RectCoord> FindAllImageBounds()
    {
        int w = wrtBitmap.PixelSize.Width;
        int h = wrtBitmap.PixelSize.Height;
        bool[,] visited = new bool[w, h];
        List<RectCoord> rectangles = new List<RectCoord>();

        using (var buf = wrtBitmap.Lock())
        {
            var ptr = (uint*)buf.Address;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Pixel pxl = new Pixel(ptr[y * w + x]);
                    if (!visited[x, y] && !isWhite(pxl))
                    {
                        RectCoord rect = GetBoundsOfConnectedPixels(visited, x, y);
                        rectangles.Add(rect);
                    }
                }
            }
        }
        return rectangles;
    }
    
    // другой матеод для определения границ, путем очереди
    private unsafe RectCoord GetBoundsOfConnectedPixels(bool[,] visited , int startX, int startY)
    {
        int w = wrtBitmap.PixelSize.Width;
        int h = wrtBitmap.PixelSize.Height;
        int minX = startX, minY = startY, maxX = startX, maxY = startY;
        
        Queue<Point> queue = new Queue<Point>();
        queue.Enqueue(new Point(startX, startY));
        
        // Смещения для соседей, включая диагональные и на расстоянии 2 пикселя
        int[] dx = { -2, -1, 0, 1, 2, -2, -1, 0, 1, 2, -2, -1, 0, 1, 2, -2, -1, 0, 1, 2 };
        int[] dy = { -2, -2, -2, -2, -2, -1, -1, -1, -1, -1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2 };
        
        using (var buf = wrtBitmap.Lock())
        {
            var ptr = (uint*)buf.Address;
            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                int x = (int)p.X;
                int y = (int)p.Y;
            
                if (x < 0 || x >= w || y < 0 || y >= h || visited[x, y])
                    continue;
                
                Pixel pxl = new Pixel(ptr[y * w + x]);
                if (isWhite(pxl))
                    continue;
                visited[x, y] = true;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                for (int i = 0; i < dx.Length; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny])
                    {
                        queue.Enqueue(new Point(nx, ny));
                    }
                }
                
            }
        }
        return new RectCoord(minX, minY, maxX, maxY);

    }
    
    // Отрисовка выделения фото (отрисовка прямоугольника)
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
    
    
    
    
    
    
    
    
    // Обновляем картинку
    private void Update()
    {
        BitmapImage = wrtBitmap;
        TriggerMyEvent();
    }

    private bool isWhite(Pixel _pxl)
    {
        return _pxl.GetPixelArgb(ColorChannel.R) >= WhiteSensivity &&
               _pxl.GetPixelArgb(ColorChannel.G) >= WhiteSensivity &&
               _pxl.GetPixelArgb(ColorChannel.B) >= WhiteSensivity;
    }

    // Класс для хранения угловых координат границ 
    public class RectCoord
    {
        private int minX;
        private int minY;
        private int maxX;
        private int maxY;
        
        public RectCoord(int _x, int _y, int _xx, int _yy)
        {
            minX = _x;
            minY = _y;
            maxX = _xx;
            maxY = _yy;
        }
        
        public (int, int, int, int) GetCoord()
        {
            return (minX, minY, maxX, maxY);
        }
    }
}