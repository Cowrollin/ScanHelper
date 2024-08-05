using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs.Internal;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using static ScanHelper.Pixel;

namespace ScanHelper.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private Bitmap? _ScanImage;
    [ObservableProperty] 
    private WriteableBitmap _bitmapImage;
    public ObservableCollection<PreviewImage> PreviewImages { get; set; }

    private WriteableBitmap wrtBitmap;
    private byte WhiteSensivity = 240;
    private int MinWidthPhoto = 600;
    private int MinHeightPhoto = 600;

    public event EventHandler MyEvent;
    
    public MainWindowViewModel()
    {
        PreviewImages = new ObservableCollection<PreviewImage>();
        wrtBitmap = new WriteableBitmap(new PixelSize(100, 100), new Vector(100, 100), PixelFormat.Bgra8888);
        Update();
    }
    
    public void TriggerMyEvent()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
    
    // Add Scan 
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
                    _ScanImage.CopyPixels(new Avalonia.PixelRect(0,0, _ScanImage.PixelSize.Width, _ScanImage.PixelSize.Height), lockbmp.Address, lockbmp.RowBytes * lockbmp.Size.Height, lockbmp.RowBytes);
                }
            }
        }
        Update();
    }

    // run algorythm
    public void SetButtonClicked()
    {
        RedRectangle();
        Update();
    }
    
    // save images
    public void SaveButtonClicked()
    {
        int c = 0;
        foreach (var item in PreviewImages)
        {
            if (item.IsChecked)
            {
                c++;
                item.Image.Save($"Photo_{c}.png");
            }
        }
    }
    
    
    // Выделяем
    private void RedRectangle()
    {
        List<PixelRect> imageBounds = FindAllImageBounds();
        foreach (var item in imageBounds)
        {
            if (item.Width > MinWidthPhoto && item.Height > MinHeightPhoto)
            {
                PreviewImage pi = new PreviewImage();
                pi.Image = GetPhotoFromCoord(item);
                pi.IsChecked = true;
                PreviewImages.Add(pi);
                Console.WriteLine("!");
            }
        }
    }

    // ищем первый пиксель картинки и вызываем метод GetBoundsOfConnectedPixels
    private unsafe List<PixelRect> FindAllImageBounds()
    {
        int w = wrtBitmap.PixelSize.Width;
        int h = wrtBitmap.PixelSize.Height;
        bool[,] visited = new bool[w, h];
        List<PixelRect> rectangles = new List<PixelRect>();

        using (var buf = wrtBitmap.Lock())
        {
            var ptr = (uint*)buf.Address;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Pixel pxl = new Pixel(ptr[y * w + x]);
                    if (!visited[x, y] && !pxl.isWhite(WhiteSensivity))
                    {
                        PixelRect pixelRect = GetBoundsOfConnectedPixels(visited, x, y);
                        rectangles.Add(pixelRect);
                    }
                }
            }
        }
        return rectangles;
    }
    
    // метод для определения границ, путем очереди
    private unsafe PixelRect GetBoundsOfConnectedPixels(bool[,] visited , int startX, int startY)
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
                if (pxl.isWhite(WhiteSensivity))
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
        return new PixelRect(minX, minY, maxX - minX, maxY - minY);

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

    // Копируем пиксели с основной картинки на другую
    private WriteableBitmap GetPhotoFromCoord(PixelRect _coord)
    {
        WriteableBitmap resultBitmap = new WriteableBitmap(_coord.Size, _ScanImage.Dpi, PixelFormat.Bgra8888);;
        
        int w = resultBitmap.PixelSize.Width;
        int h = resultBitmap.PixelSize.Height;

        using (var buf = resultBitmap.Lock())
        {
            using (var buf2 = wrtBitmap.Lock())
            {
                unsafe
                {
                    var ptr = (uint*)buf.Address;
                    var ptr2 = (uint*)buf2.Address;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            ptr[y * w + x] = ptr2[(y + _coord.Y) * wrtBitmap.PixelSize.Width + x + _coord.X];
                        }
                    }
                }
            }
        }

        return resultBitmap;
    }
    
    // Save photos in file
    private async Task SavePhotos(WriteableBitmap wbitmap)
    {
        /*if (wbitmap == null)
            throw new ArgumentNullException(nameof(wbitmap));
        
        var saveFileDialog = new SaveFileDialog
        {
            DefaultExtension = "png",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "PNG", Extensions = new List<string> { "png" } },
                new FileDialogFilter { Name = "JPEG", Extensions = new List<string> { "jpg", "jpeg" } },
                new FileDialogFilter { Name = "BMP", Extensions = new List<string> { "bmp" } }
            }
        };

        var filePath = await saveFileDialog.ShowAsync(new Window());

        if (!string.IsNullOrEmpty(filePath))
        {
            // Создание директории, если она не существует
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Сохранение изображения в файл
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            { 
                wbitmap.Save(fileStream);
            }
        }*/
    }
    
    // Обновляем изображение
    private void Update()
    {
        BitmapImage = wrtBitmap;
        TriggerMyEvent();
    }
}

public partial class PreviewImage : ObservableObject
{
    [ObservableProperty]
    private WriteableBitmap image;

    [ObservableProperty]
    private bool isChecked;
}