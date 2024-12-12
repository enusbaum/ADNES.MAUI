using System.Reflection;

namespace ADNES.MAUI.Extensions
{
    /// <summary>
    ///     Generic extension method to get an attribute from an enum based on the attribute type
    /// </summary>
    public static class EnumExtensions
    {
        public static TAttribute? GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name == null)
                return null;

            var field = type.GetField(name);
            return field?.GetCustomAttribute<TAttribute>();
        }
    }
}