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
        public SKBitmap Image;

        /// <summary>
        ///     The original loaded image. We use this to reset back to our original state
        /// </summary>
        private readonly SKBitmap _originalImage;

        /// <summary>
        ///     Original size of the original image when loaded
        /// </summary>
        private SKSize _originalImageSize => new(Image.Width, Image.Height);

        /// <summary>
        ///     Original location of the touch areas before being scaled (we keep this for reference)
        /// </summary>
        private readonly Dictionary<int, SKRect>? _originalAreas;

        /// <summary>
        ///     Dictionary of Touch Areas and their Rectangles for touch events
        /// </summary>
        public Dictionary<int, SKRect> Areas { get; set; } = new();

        /// <summary>
        ///     Task used for rendering the layers on the image
        /// </summary>
        private readonly Task _layerRenderingTask;

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
            _originalImage = Task.Run(async () => await GetSKBitmapFromResourceAsync(resourceName)).GetAwaiter()
                .GetResult();

            Image = _originalImage.Copy();

            //Set our reference for the original areas, as well as the currently defined areas
            _originalAreas = imageAreas;
            ResetAreas();

            _layerRenderingTask = Task.Factory.StartNew(LayerRender);
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

            var widthRatio = scaledImageSize.Width / _originalImageSize.Width;
            var heightRatio = scaledImageSize.Height / _originalImageSize.Height;

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
        public void LayerRender()
        {
            while (true)
            {
                Task.Delay(33); //~29.97fps -- NTSC

                //No Layers to render?
                if (Layers.Count == 0)
                    continue;

                //Check to see if any Layers have timed out, so we can remove them and re-render
                foreach (var layer in Layers.ToList())
                {
                    //Infinite Display
                    if (layer.DisplayDuration <= 0)
                        continue;

                    if (layer.DisplayStart.AddMilliseconds(layer.DisplayDuration) < DateTime.Now) 
                        Layers.Remove(layer);
                }

                //All current Layers have already been rendered? Also catches where an layer has been removed
                if (_renderedLayerCount == Layers.Count)
                    continue;

                Image = _originalImage.Copy();

                //Draw the layer on image, starting with the original image
                using var canvas = new SKCanvas(Image);

                foreach (var layer in Layers)
                {
                    //We draw the layer on the full resolution Image, so we don't need to worry about scaling
                    //The application will automatically scale the image and the layer will scale along with it
                    canvas.DrawBitmap(layer.Image, layer.Location);

                    _renderedLayerCount++;
                }

                canvas.Save();
            }
        }

        /// <summary>
        ///     IDisposable implementation
        /// </summary>
        public void Dispose()
        {
            _layerRenderingTask.Dispose();
        }
    }
}
