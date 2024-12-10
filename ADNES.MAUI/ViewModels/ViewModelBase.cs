using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ADNES.MAUI.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {

        /// <summary>
        ///     Event Handler for Property Changed (INotifyPropertyChanged)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///    Method to raise the PropertyChanged event (INotifyPropertyChanged)
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Capture the event to avoid race conditions
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
