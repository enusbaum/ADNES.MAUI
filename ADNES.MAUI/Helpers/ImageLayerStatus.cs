namespace ADNES.MAUI.Helpers
{
    /// <summary>
    ///     Enum to specify the current status of an Image Layer
    /// </summary>
    public enum ImageLayerStatus
    {
        /// <summary>
        ///     Delayed, do not display this layer
        /// </summary>
        Delay,

        /// <summary>
        ///     Display this layer
        /// </summary>
        Display,

        /// <summary>
        ///     Expired, do not display and layer can be removed
        /// </summary>
        Expired
    }
}
