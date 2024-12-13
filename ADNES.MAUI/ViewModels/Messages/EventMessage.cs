using ADNES.MAUI.ViewModels.Enums;

namespace ADNES.MAUI.ViewModels.Messages
{
    /// <summary>
    ///     Class to be used by the WeakReferenceMessenger to send messages to the View from the ViewModel
    /// </summary>
    public record EventMessage
    {
        /// <summary>
        ///     The event to be raised
        /// </summary>
        public RedrawEvents RedrawEvent { get; set; }
    }
}
