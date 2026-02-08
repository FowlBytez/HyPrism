using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace HyPrism.UI.Helpers;

public static class BitmapHelper
{
    /// <summary>
    /// Creates a high-quality scaled copy of the bitmap.
    /// Used to fix rendering artifacts when downscaling large images for thumbnails/avatars.
    /// </summary>
    public static Bitmap CreateResized(this Bitmap source, int width, int height)
    {
        try
        {
            return source.CreateScaledBitmap(new PixelSize(width, height), BitmapInterpolationMode.HighQuality);
        }
        catch
        {
            return source;
        }
    }

    /// <summary>
    /// Loads a bitmap from a URI string (resource or file) and optionally resizes it during decode 
    /// to save memory.
    /// </summary>
    public static Bitmap? LoadBitmap(string uriString, int? decodeWidth = null)
    {
        try
        {
            var uri = new Uri(uriString);
            using var stream = AssetLoader.Open(uri);
            
            if (decodeWidth.HasValue)
            {
                // Decode to specific width to save memory
                return Bitmap.DecodeToWidth(stream, decodeWidth.Value, BitmapInterpolationMode.HighQuality);
            }
            
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

