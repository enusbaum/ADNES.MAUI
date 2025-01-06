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
            using var font = new SKFont(SKTypeface.FromStream(fileStream), size.Height);

            //Measure the text and reduce the font size until it fits the width of the bitmap
            while (font.MeasureText(text, paint) > size.Width)
                font.Size--;

            var textWidth = font.MeasureText(text, paint);
            var textHeight = font.Size;

            // Compute coordinates to center the text
            var x = (size.Width - textWidth) / 2f;
            var y = (size.Height / 2f) + (textHeight / 2f);

            // Draw the text at the computed coordinates
            canvas.DrawText(text, x, y, font, paint);

            return bitmap;
        }

        /// <summary>
        ///     Renders an SKBitmap with a pause graphic (two vertical bars) in the center of the specified size with
        ///     the specified background and foreground colors.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="foregroundColor"></param>
        /// <returns></returns>
        public SKBitmap RenderPauseGraphic(SKSize size, SKColor backgroundColor, SKColor foregroundColor)
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

            // Compute the coordinates for the pause graphic
            var barWidth = size.Width / 10f;
            var barHeight = size.Height / 2f;
            var leftBarX = (size.Width / 2f) - (barWidth * 2f);
            var rightBarX = (size.Width / 2f) + barWidth;
            var barY = (size.Height / 2f) - (barHeight / 2f);

            // Draw the pause graphic
            canvas.DrawRect(leftBarX, barY, barWidth, barHeight, paint);
            canvas.DrawRect(rightBarX, barY, barWidth, barHeight, paint);
            return bitmap;
        }

        /// <summary>
        ///    Renders an SKBitmap with a circle that fills the bitmap area based on the specified percent
        /// </summary>
        /// <param name="size"></param>
        /// <param name="borderColor"></param>
        /// <param name="fillColor"></param>
        /// <param name="borderWidth"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static SKBitmap RenderCircle(SKSize size,
                                                 SKColor borderColor,
                                                 SKColor fillColor,
                                                 float borderWidth,
                                                 int percent)
        {
            // Clamp percent to the range [0, 100]
            percent = Math.Max(0, Math.Min(100, percent));

            // Create a new bitmap
            var bmpWidth = (int)size.Width;
            var bmpHeight = (int)size.Height;
            var bitmap = new SKBitmap(bmpWidth, bmpHeight);

            using var canvas = new SKCanvas(bitmap);
            // Clear the canvas (you can also use transparent if you prefer)
            canvas.Clear(SKColors.Transparent);

            // Calculate the maximum possible radius for the circle to "fill" the bitmap area.
            // Usually, we let the circle fit inside the rectangle, so we pick half of the smaller dimension.
            var maxRadius = Math.Min(size.Width, size.Height) / 2f;

            // Convert percent [0..100] to a radius [~1..maxRadius].
            //  - 0% should be ~1 pixel so it's still visible
            //  - 100% should be maxRadius
            var radius = 1 + (maxRadius - 1) * (percent / 100f);

            // Calculate center of the bitmap
            var centerX = size.Width / 2f;
            var centerY = size.Height / 2f;

            // Create paint for filling
            using (var fillPaint = new SKPaint())
            {
                fillPaint.Color = fillColor;
                fillPaint.IsAntialias = true;
                fillPaint.Style = SKPaintStyle.Fill;
                // Draw filled circle
                canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            }

            // Create paint for border
            using (var borderPaint = new SKPaint())
            {
                borderPaint.Color = borderColor;
                borderPaint.StrokeWidth = borderWidth;
                borderPaint.IsAntialias = true;
                borderPaint.Style = SKPaintStyle.Stroke;
                // Draw circle border
                canvas.DrawCircle(centerX, centerY, radius, borderPaint);
            }

            return bitmap;
        }

        /// <summary>
        ///     Returns a list of SKBitmaps that render an expanding circle effect using the RenderCircle method
        ///
        ///     The total number of frames are passed into the method to determine the size of the expanding circle per frame
        ///     starting at a percent of 0 and ending at 100.
        /// </summary>
        /// <param name="numberOfFrames"></param>
        /// <returns></returns>
        public List<SKBitmap> RenderExpandingCircles(int numberOfFrames, SKSize size, SKColor borderColor, SKColor fillColor)
        {
            var bitmaps = new List<SKBitmap>();
            for (var i = 0; i < numberOfFrames; i++)
            {
                var percent = (int)Math.Round((i / (float)numberOfFrames) * 100);
                bitmaps.Add(RenderCircle(size, borderColor, fillColor, 2, percent));
            }
            return bitmaps;
        }
    }
}
