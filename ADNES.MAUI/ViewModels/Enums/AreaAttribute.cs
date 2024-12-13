using SkiaSharp;

namespace ADNES.MAUI.ViewModels.Enums
{
    /// <summary>
    ///     Attribute to define the area of the console that can be interacted with or drawn on
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    [AttributeUsage(AttributeTargets.Field)]
    public class AreaAttribute(float left, float top, float right, float bottom) : Attribute
    {
        public SKRect Rect { get; } = new(left, top, right, bottom);

    }
}
