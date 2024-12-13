namespace ADNES.MAUI.ViewModels.Enums
{
    /// <summary>
    ///     Enum to define the areas of the controller that can be interacted with or drawn on
    /// </summary>
    public enum ControllerAreas
    {
        [Area(92, 80, 154, 127)]
        DPadUp,

        [Area(92, 179, 154, 225)]
        DPadDown,

        [Area(49, 122, 97, 183)]
        DPadLeft,

        [Area(150, 122, 195, 183)]
        DPadRight,

        [Area(258, 176, 313, 201)]
        SelectButton,

        [Area(347, 176, 402, 201)]
        StartButton,

        [Area(467, 150, 548, 228)]
        AButton,

        [Area(561, 150, 642, 228)]
        BButton
    }
}
