using SkiaSharp;

namespace ADNES.MAUI.Helpers
{
    /// <summary>
    ///     Small helper class to manage SKBitmaps and caching from resources
    /// </summary>
    public class SkiaHelpers
    {
        private readonly Dictionary<string, SKBitmap> _bitmapCache = new();

        /// <summary>
        ///    Retrieves a SKBitmap from a MAUI RAW resource
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<SKBitmap> GetSKBitmapFromResource(string fileName)
        {
            if (_bitmapCache.TryGetValue(fileName, out var bitmap)) return bitmap;

            await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            bitmap = SKBitmap.Decode(memoryStream.ToArray());
            _bitmapCache[fileName] = bitmap;

            return bitmap;
        }

    }
}