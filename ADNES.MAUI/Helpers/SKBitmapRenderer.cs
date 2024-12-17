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
            // Create a bitmap and associated canvas
            var info = new SKImageInfo((int)size.Width, (int)size.Height);
            var bitmap = new SKBitmap(info);
            using var canvas = new SKCanvas(bitmap);

            // Clear the background
            canvas.Clear(backgroundColor);

            // Set up the paint
            using var paint = new SKPaint();
            paint.Color = foregroundColor;
            paint.IsAntialias = true;

            // Create an SKFont
            var fileStream = FileSystem.OpenAppPackageFileAsync("nintendo-nes-font.ttf").GetAwaiter().GetResult();
            using var font = new SKFont(SKTypeface.FromStream(fileStream), 24);

            // Measure the text to center it
            var textWidth = font.MeasureText(text, paint);
            var textHeight = font.Size;

            // Compute coordinates to center the text
            var x = (size.Width - textWidth) / 2f;
            var y = (size.Height / 2f) + (textHeight / 2f);

            // Draw the text at the computed coordinates
            canvas.DrawText(text, x, y, font, paint);

            return bitmap;
        }
    }
}
