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
        ///     Specifies the current status of this layer
        /// </summary>
        public ImageLayerStatus Status
        {
            get
            {
                //Check to see if we're delayed looking at DisplayDelay and LayerAddedTimestamp
                if (DisplayDelay > 0 && DateTime.Now < LayerAddedTimestamp.AddMilliseconds(DisplayDelay))
                    return ImageLayerStatus.Delay;

                //Check to see if we're expired looking at DisplayDuration and LayerAddedTimestamp
                if (DisplayDuration > 0 && DateTime.Now > LayerAddedTimestamp.AddMilliseconds(DisplayDelay + DisplayDuration))
                    return ImageLayerStatus.Expired;

                //If we're not delayed or expired, then we're ready to display
                return ImageLayerStatus.Display;
            }
        }

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
        public DateTime LayerAddedTimestamp { get; set; }

        /// <summary>
        ///    The delay in milliseconds before the layer should be displayed on top of the base image
        ///
        ///     Delay is calculated from the LayerAddedTimestamp
        /// </summary>
        public int DisplayDelay { get; set; }

        /// <summary>
        ///     The duration in milliseconds that the layer should be rendered on top of the base image
        ///
        ///     This is used for temporary messages or notifications
        ///
        ///     Duration of 0 means the layer will be displayed indefinitely
        ///
        ///     Duration is calculated from the LayerAddedTimestamp + DisplayDelay (if any)
        /// </summary>
        public int DisplayDuration { get; set; }
    }
}
