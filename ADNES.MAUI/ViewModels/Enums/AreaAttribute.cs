using SkiaSharp;

namespace ADNES.MAUI.ViewModels.Enums
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AreaAttribute(float left, float top, float right, float bottom) : Attribute
    {
        public SKRect Rect { get; } = new(left, top, right, bottom);

    }
}
