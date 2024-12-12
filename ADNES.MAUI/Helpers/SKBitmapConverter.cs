using System.Runtime.CompilerServices;
using SkiaSharp;

namespace ADNES.MAUI.Helpers
{
    /// <summary>
    ///     Converter that generates SKBitmaps from input data from ADNES
    /// </summary>
    public class SKBitmapConverter
    {
        private readonly SKBitmap _bitmap = new(new SKImageInfo(256, 240));
        private readonly SKColor[] _colorPalette;
        private readonly Random _random = new(DateTime.Now.GetHashCode());

        /// <summary>
        ///     Constructor that takes a pre-defined Color Palette
        /// </summary>
        /// <param name="palette"></param>
        public SKBitmapConverter(System.Drawing.Color[] palette)
        {
            // We convert a pre-defined 8-bit color palette to SKColor for easy rendering
            _colorPalette = palette.Select(c => new SKColor(c.R, c.G, c.B, c.A)).ToArray();
        }

        /// <summary>
        ///     Takes the input 8bpp bitmap and renders it as a SKBitmap
        ///     using the pre-defined Color Palette
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public SKBitmap Render(Span<byte> bitmap)
        {
            for (var y = 0; y < 240; y++)
            {
                for (var x = 0; x < 256; x++)
                {
                    _bitmap.SetPixel(x, y, _colorPalette[bitmap[y * 256 + x]]);
                }
            }
            return _bitmap;
        }

        /// <summary>
        ///     Renders a black/white noise pattern
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public Span<byte> GenerateNoise(Span<byte> buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = _random.Next(0, 10) <= 5 ? (byte)0xd : (byte)0x30;
            }
            return buffer;
        }
    }
}
