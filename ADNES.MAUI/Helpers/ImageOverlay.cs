using SkiaSharp;

namespace ADNES.MAUI.Helpers
{
    public class ImageOverlay
    {
        /// <summary>
        ///     Unique identifier for the overlay
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        ///     The Image to be drawn over the base image on top of the base Image
        /// </summary>
        public SKBitmap Image { get; set; }

        /// <summary>
        ///     The location on the base image to start drawing the overlay image (Top Left)
        /// </summary>
        public SKPoint Location { get; set; }

        /// <summary>
        ///     Timestamp of when the overlay was added
        /// </summary>
        public DateTime DisplayStart { get; set; }

        /// <summary>
        ///     The duration in milliseconds that the overlay should be displayed
        ///
        ///     Duration of 0 means the overlay will be displayed indefinitely
        /// </summary>
        public int DisplayDuration { get; set; }
    }
}
