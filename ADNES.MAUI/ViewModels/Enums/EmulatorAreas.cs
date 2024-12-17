namespace ADNES.MAUI.ViewModels.Enums
{
    /// <summary>
    ///     Enum to define the areas of the emulator that can be interacted with or drawn on
    /// </summary>
    public enum EmulatorAreas
    {
        /// <summary>
        ///     Area that covers the entire screen
        /// </summary>
        [Area(0,0, 256, 240)]
        FullScreen,

        /// <summary>
        ///     Area that is a 20x20 box in the top left corner of the screen for FPS display
        /// </summary>
        [Area(0, 0, 20, 20)]
        TopLeftFPS,

        /// <summary>
        ///     Area that is a 20x20 box in the top right corner of the screen for FPS display
        /// </summary>
        [Area(236, 0, 256, 20)]
        TopRightFPS,

        /// <summary>
        ///    Area that is a 20x20 box in the bottom right corner of the screen for RPS display
        /// </summary>
        [Area(0, 220, 20, 240)]
        BottomLeftFPS,

        /// <summary>
        ///    Area that is a 20x20 box in the bottom right corner of the screen for RPS display
        /// </summary>
        [Area(236, 220, 256, 240)]
        BottomRightRPS,

        /// <summary>
        ///     Area that is 40 pixels tall across the entire center of the screen for message banners to appear
        /// </summary>
        [Area(0, 100, 256, 140)]
        CenterBanner
    }
}
