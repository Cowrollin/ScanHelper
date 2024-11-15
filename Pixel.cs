using System;
namespace ScanHelper;

public enum ColorChannel
{
    A,
    R,
    G,
    B
}

public struct Pixel
{
    private uint pixelValue;

    public Pixel(uint pixelValue)
    {
        this.pixelValue = pixelValue;
    }
    
    public byte GetPixelArgb(ColorChannel colorChannel)
    {
        switch (colorChannel)
        {
            case ColorChannel.A:
                return (byte)((pixelValue >> 24) & 0xFF); // Альфа-канал
            case ColorChannel.R:
                return (byte)((pixelValue >> 16) & 0xFF); // Красный канал
            case ColorChannel.G:
                return (byte)((pixelValue >> 8) & 0xFF); // Зеленый канал
            case ColorChannel.B:
                return (byte)(pixelValue & 0xFF); // Синий канал
            default:
                throw new ArgumentException("Invalid color channel");
        }
    }
    
    public bool isWhite(byte WhiteSensivity)
    {
        return GetPixelArgb(ColorChannel.R) >= WhiteSensivity &&
               GetPixelArgb(ColorChannel.G) >= WhiteSensivity &&
               GetPixelArgb(ColorChannel.B) >= WhiteSensivity;
    }
}