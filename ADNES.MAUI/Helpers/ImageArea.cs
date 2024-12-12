using SkiaSharp;

namespace ADNES.MAUI.Helpers
{
    /// <summary>
    ///     The ImageArea class is used to load an image resource from the MAUI project into an SKBitmap, and then allow
    ///     the user to define specified areas within the image that can be used for touch events, drawing, etc.
    ///
    ///     The user can pass into this class new dimensions for the image (if the displayed image is scaled depending on the device),
    ///     and it recalculates the location of the specified areas within the image based on the new dimensions.
    /// </summary>
    public class ImageArea
    {
        /// <summary>
        ///    Image to be used for the touch areas
        /// </summary>
        public SKBitmap Image;

        /// <summary>
        ///     Original size of the original image when loaded
        /// </summary>
        private SKSize OriginalImageSize => new(Image.Width, Image.Height);

        /// <summary>
        ///     Size of the image after being scaled
        /// </summary>
        private SKSize _imageSize;

        /// <summary>
        ///     Size of the image after being scaled
        /// </summary>
        public SKSize ImageSize
        {
            get => _imageSize;
            set
            {
                _imageSize = value;
                CalculateAreas(value);
            }
        }

        /// <summary>
        ///     Original location of the touch areas before being scaled (we keep this for reference)
        /// </summary>
        private readonly Dictionary<int, SKRect>? _originalAreas;

        /// <summary>
        ///     Dictionary of Touch Areas and their Rectangles for touch events
        /// </summary>
        public Dictionary<int, SKRect> Areas { get; set; } = new();

        public ImageArea(string resourceName, Dictionary<int, SKRect>? imageAreas = null)
        {
            //Set Image
            Image = Task.Run(async () => await GetSKBitmapFromResourceAsync(resourceName)).GetAwaiter()
                .GetResult();

            //Set initial image size to the original image size
            ImageSize = OriginalImageSize;

            //Set our reference for the original areas, as well as the currently defined areas
            _originalAreas = imageAreas;
            ResetAreas();
        }

        private void ResetAreas()
        {
            if (_originalAreas != null)
                Areas = new Dictionary<int, SKRect>(_originalAreas);
        }

        /// <summary>
        ///     Determines if the point is within any of the touch areas and returns the key
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int InArea(SKPoint point)
        {
            if (!Areas.Any(x => x.Value.Contains(point)))
                return -1;

            return Areas.FirstOrDefault(x => x.Value.Contains(point)).Key;
        }

        /// <summary>
        ///     Determines if the point is within any of the touch areas and returns the key
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int InArea(float x, float y) => InArea(new SKPoint(x, y));

        /// <summary>
        ///     Scales the areas to the new image size on the view
        /// </summary>
        /// <param name="scaledImageSize"></param>
        public void CalculateAreas(SKSize scaledImageSize)
        {
            //No areas were set, so we can't calculate anything
            if (_originalAreas == null)
                return;

            ResetAreas();

            var widthRatio = scaledImageSize.Width / OriginalImageSize.Width;
            var heightRatio = scaledImageSize.Height / OriginalImageSize.Height;

            foreach (var area in Areas)
            {
                Areas[area.Key] = new SKRect(area.Value.Left * widthRatio, area.Value.Top * heightRatio,
                    area.Value.Right * widthRatio, area.Value.Bottom * heightRatio);
            }
        }

        /// <summary>
        ///    Retrieves a SKBitmap from a MAUI RAW resource
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<SKBitmap> GetSKBitmapFromResourceAsync(string fileName)
        {

            await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var bitmap = SKBitmap.Decode(memoryStream.ToArray());

            return bitmap;
        }

        /// <summary>
        ///     Sync version of GetSKBitmapFromResourceAsync
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static SKBitmap GetSKBitmapFromResource(string fileName) => GetSKBitmapFromResourceAsync(fileName).Result;
    }
}
