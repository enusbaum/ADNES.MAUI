namespace ADNES.MAUI.ViewModels.Enums
{
    /// <summary>
    ///     Enum to represent the different areas of the console that can be interacted with or drawn on
    /// </summary>
    public enum ConsoleAreas
    {
        [Area(146, 264, 163, 275)]
        PowerLED,

        [Area(185, 250, 280, 296)]
        PowerButton,

        [Area(300, 250, 400, 296)]
        ResetButton,

        [Area(150, 0, 780, 135)]
        Cartridge
    }
}
