using System.Runtime.CompilerServices;
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
    public class ImageArea : IDisposable
    {
        /// <summary>
        ///    Image to be used for the touch areas
        /// </summary>
        private SKBitmap _image;

        public SKBitmap Image
        {
            get
            {
                LayerRender();

                return _image;
            }
            set => _image = value;
        }

        /// <summary>
        ///     The base image. We use this to reset back to our original state
        /// </summary>
        private SKBitmap _baseImage;

        /// <summary>
        ///     Original size of the base image when loaded
        /// </summary>
        private SKSize _baseImageSize => new(Image.Width, Image.Height);

        /// <summary>
        ///     Original location of the touch areas before being scaled (we keep this for reference)
        /// </summary>
        private readonly Dictionary<int, SKRect>? _baseAreas;

        /// <summary>
        ///     Dictionary of Touch Areas and their Rectangles for touch events
        /// </summary>
        public Dictionary<int, SKRect> Areas { get; set; } = new();

        /// <summary>
        ///     Layers to be rendered on top of the image
        /// </summary>
        public List<ImageLayer> Layers { get; set; } = [];

        /// <summary>
        ///     The number of layers currently rendered to the Image
        /// </summary>
        private int _renderedLayerCount = 0;

        public ImageArea(string resourceName, Dictionary<int, SKRect>? imageAreas = null)
        {
            //Set Image
            _baseImage = Task.Run(async () => await GetSKBitmapFromResourceAsync(resourceName)).GetAwaiter()
                .GetResult();

            Image = _baseImage.Copy();

            //Set our reference for the original areas, as well as the currently defined areas
            _baseAreas = imageAreas;
            ResetAreas();
        }

        public ImageArea(SKBitmap image, Dictionary<int, SKRect>? imageAreas = null)
        {
            _baseImage = image;
            Image = _baseImage.Copy();

            //Set our reference for the original areas, as well as the currently defined areas
            _baseAreas = imageAreas;
            ResetAreas();
        }

        public void SetBaseImage(SKBitmap baseImage)
        {
            //Verify the new image has the same dimensions as the original image
            if (baseImage.Width != _baseImage.Width || baseImage.Height != _baseImage.Height)
                throw new Exception("New image dimensions do not match the original image dimensions");

            _baseImage = baseImage;

            LayerRender(true);
        }

        private void ResetAreas()
        {
            if (_baseAreas != null)
                Areas = new Dictionary<int, SKRect>(_baseAreas);
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
            if (_baseAreas == null)
                return;

            ResetAreas();

            var widthRatio = scaledImageSize.Width / _baseImageSize.Width;
            var heightRatio = scaledImageSize.Height / _baseImageSize.Height;

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
        public static SKBitmap GetSKBitmapFromResource(string fileName) => GetSKBitmapFromResourceAsync(fileName).GetAwaiter().GetResult();

        /// <summary>
        ///     Adds a Layer to be rendered on top of the image
        ///
        ///     The Id returned is a GUID used to reference the Layer if the user wants to manually remove it
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="location"></param>
        /// <param name="displayDuration"></param>
        public Guid AddLayer(SKBitmap bitmap, SKPoint location, int displayDuration = 0)
        {
            var id = Guid.NewGuid();

            Layers.Add(new ImageLayer()
            {
                Id = id,
                Image = bitmap,
                Location = location,
                DisplayStart = DateTime.Now,
                DisplayDuration = displayDuration
            });

            return id;
        }

        /// <summary>
        ///     Removes the layer from the list of Layers to be rendered
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool RemoveLayer(Guid id)
        {
            var layer = Layers.FirstOrDefault(x => x.Id == id);

            if (layer == null)
                return false;

            Layers.Remove(layer);

            return true;
        }

        /// <summary>
        ///     Task to handle rendering Layers on to the Image
        /// </summary>
        /// <param name="forceRender">Forces a Layer Render, skipping checks to see if it's needed</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void LayerRender(bool forceRender = false)
        {
            //If we're not forcing a render, check to see if we even need to do this
            if (!forceRender)
            {
                //No Layers to render?
                if (Layers.Count == 0)
                    return;

                //All current Layers have already been rendered? Also catches where an layer has been removed
                if (_renderedLayerCount == Layers.Count)
                    return;
            }

            _image = _baseImage.Copy();

            //Draw the layer on image, starting with the original image
            using var canvas = new SKCanvas(_image);

            foreach (var layer in Layers)
            {
                //If the layer has a display duration, we check if it has expired, if so just skip. Another process will clean up expired layers
                if (layer.DisplayDuration > 0 && DateTime.Now.Subtract(layer.DisplayStart).TotalMilliseconds > layer.DisplayDuration)
                    continue;

                //We draw the layer on the full resolution Image, so we don't need to worry about scaling
                //The application will automatically scale the image and the layer will scale along with it
                canvas.DrawBitmap(layer.Image, layer.Location);

                _renderedLayerCount++;
            }

            canvas.Save();
        }

        /// <summary>
        ///     IDisposable implementation
        /// </summary>
        public void Dispose()
        {
        }
    }
}
