using SkiaSharp;

namespace ADNES.MAUI.Helpers
{
    /// <summary>
    ///     Class used for Rendering SKBitmaps with various visual elements/effects
    /// </summary>
    public class SKBitmapRenderer
    {
        /// <summary>
        ///     Renders an SKBitmap that is filled with a single solid color conforming to the specified size
        /// </summary>
        /// <param name="size"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public SKBitmap RenderSolidColor(SKSize size, SKColor color)
        {
            var bitmap = new SKBitmap(new SKImageInfo((int)size.Width, (int)size.Height));
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(color);
            return bitmap;
        }

        /// <summary>
        ///     Renders an SKBitmap with a specified text string, background color, and foreground color
        /// </summary>
        /// <param name="size"></param>
        /// <param name="text"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="foregroundColor"></param>
        /// <returns></returns>
        public SKBitmap RenderText(SKSize size, string text, SKColor backgroundColor, SKColor foregroundColor)
        {
            var bitmap = new SKBitmap(new SKImageInfo((int)size.Width, (int)size.Height));
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(backgroundColor);
            using var paint = new SKPaint
            {
                Color = foregroundColor,
                IsAntialias = true
            };
            canvas.DrawText(text, size.Width / 2, size.Height / 2, paint);
            return bitmap;

        }
    }
}
