using ADNES.MAUI.ViewModels.Enums;

namespace ADNES.MAUI.ViewModels.Messages
{
    public record EventMessage
    {
        public RedrawEvents RedrawEvent { get; set; }
    }
}
