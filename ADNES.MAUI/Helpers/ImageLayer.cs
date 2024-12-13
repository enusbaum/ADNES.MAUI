using SkiaSharp;

namespace ADNES.MAUI.Helpers
{
    public class ImageLayer
    {
        /// <summary>
        ///     Unique identifier for the Layer
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        ///     The Image to be drawn over the base image on top of the base Image
        /// </summary>
        public SKBitmap Image { get; set; }

        /// <summary>
        ///     The location on the base image to start drawing the Layer image (Top Left)
        /// </summary>
        public SKPoint Location { get; set; }

        /// <summary>
        ///     Timestamp of when the layer was added
        /// </summary>
        public DateTime DisplayStart { get; set; }

        /// <summary>
        ///     The duration in milliseconds that the layer should be rendered on top of the base image
        ///
        ///     This is used for temporary messages or notifications
        ///
        ///     Duration of 0 means the layer will be displayed indefinitely
        /// </summary>
        public int DisplayDuration { get; set; }
    }
}
