namespace ADNES.MAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var appWindow = new Window(new AppShell());


#if WINDOWS
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                appWindow.Width = 400;
                appWindow.Height = 800;
            }
#endif

            return appWindow;
            
        }
    }
}