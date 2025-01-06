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
    public class LayeredImage : IDisposable
    {
        /// <summary>
        ///     Lock used to prevent multiple threads from rendering the image resources while a render is happening
        /// </summary>
        private readonly Lock _renderLock = new();

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

        /// <summary>
        ///     Pixel Density of the device
        ///
        ///     Used to properly calculate the touch areas when the image is scaled
        /// </summary>
        public double PixelDensity = 0;

        public LayeredImage(string resourceName, Dictionary<int, SKRect>? imageAreas = null)
        {
            //Set Image
            _baseImage = Task.Run(async () => await GetSKBitmapFromResourceAsync(resourceName)).GetAwaiter()
                .GetResult();

            Image = _baseImage.Copy();

            //Set our reference for the original areas, as well as the currently defined areas
            _baseAreas = imageAreas;
            ResetAreas();
        }

        public LayeredImage(SKBitmap image, Dictionary<int, SKRect>? imageAreas = null)
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
        ///     Determines if the point is within any of the touch areas (taking pixel density into account) and returns the key
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int InArea(SKPoint point)
        {
            //No areas were set, so we can't calculate anything
            if (Areas.Count == 0)
                return -1;

            //Scale the point based on the pixel density
            point.X /= (float)PixelDensity;
            point.Y /= (float)PixelDensity;

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
        ///     Displays the areas on the image as a visual aid for debugging.
        ///
        ///     Areas are rendered as rectangles with a semi-transparent fill as their own layer
        /// </summary>
        /// <param name="area">Specific Area to Display (Default: -1 for all)</param>
        public void ShowAreas(int area = -1)
        {
            var bitmap = _baseImage.Copy();
            using var canvas = new SKCanvas(bitmap);

            foreach (var touchArea in Areas)
            {
                if (area != -1 && touchArea.Key != area)
                    continue;
                var paint = new SKPaint
                {
                    Color = new SKColor(255, 0, 0, 128),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(touchArea.Value, paint);
            }

            Layers.Add(new ImageLayer
            {
                Id = Guid.NewGuid(),
                Image = bitmap,
                Location = new SKPoint(0, 0)
            });
        }

        /// <summary>
        ///     Adds a Layer to be rendered on top of the image
        /// 
        ///     The ID returned is a GUID used to reference the Layer if the user wants to manually remove it
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="location"></param>
        /// <param name="displayDuration"></param>
        /// <param name="displayDelay"></param>
        public Guid AddLayer(SKBitmap bitmap, SKPoint location, int displayDuration = 0, int displayDelay = 0)
        {
            using (var scope = _renderLock.EnterScope())
            {
                var id = Guid.NewGuid();

                Layers.Add(new ImageLayer()
                {
                    Id = id,
                    Image = bitmap,
                    Location = location,
                    LayerAddedTimestamp = DateTime.Now,
                    DisplayDuration = displayDuration,
                    DisplayDelay = displayDelay
                });

                return id;
            }
        }

        /// <summary>
        ///     Adds multiple Layers to be rendered on top of the image
        /// </summary>
        /// <param name="bitmaps"></param>
        /// <param name="location"></param>
        /// <param name="displayDuration"></param>
        /// <param name="displayDelay"></param>
        /// <returns></returns>
        public List<Guid> AddLayers(IEnumerable<SKBitmap> bitmaps, SKPoint location, int displayDuration = 0, int displayDelay = 0)
        {
            using (var scope = _renderLock.EnterScope())
            {
                return bitmaps.Select(bitmap => AddLayer(bitmap, location, displayDuration, displayDelay)).ToList();
            }
        }

        /// <summary>
        ///     Removes the layer from the list of Layers to be rendered
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public void RemoveLayer(Guid id)
        {
            using (var scope = _renderLock.EnterScope())
            {
                var layer = Layers.FirstOrDefault(x => x.Id == id);

                if (layer == null)
                    return;

                Layers.Remove(layer);
            }
        }

        /// <summary>
        ///     Removes multiple layers from the list of Layers to be rendered
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public void RemoveLayers(IEnumerable<Guid> ids)
        {
            using (var scope = _renderLock.EnterScope())
            {
                foreach (var id in ids)
                {
                    var layer = Layers.FirstOrDefault(x => x.Id == id);
                    if (layer == null)
                        continue;
                    Layers.Remove(layer);
                }
            }
        }

        /// <summary>
        ///     Task to handle rendering Layers on to the Image
        /// </summary>
        /// <param name="forceRender">Forces a Layer Render, skipping checks to see if it's needed</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void LayerRender(bool forceRender = false)
        {
            using (var scope = _renderLock.EnterScope())
            {
                //If we're not forcing a render, check to see if we even need to do this
                if (!forceRender)
                {
                    //No Layers to render?
                    if (Layers.Count == 0 && _renderedLayerCount == 0)
                        return;

                    //If no layers are set to Display and no layers are already rendered, we don't need to render anything
                    if (Layers.All(x => x.Status != ImageLayerStatus.Display) && _renderedLayerCount == 0)
                        return;
                }

                _renderedLayerCount = 0;
                _image = _baseImage.Copy();

                //Draw the layer on image, starting with the original image
                using var canvas = new SKCanvas(_image);
                foreach (var layer in Layers.Where(x=> x.Status == ImageLayerStatus.Display))
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
        }
    }
}
