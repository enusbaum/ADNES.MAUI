using ADNES.MAUI.Helpers;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace ADNES.MAUI.ViewModels
{
    public partial class  EmulatorPageViewModel : ViewModelBase, IDisposable
    {
        public readonly SKBitmapRenderer BitmapRenderer;

        /// <summary>
        ///     Flag to determine if the rendering loop is running
        /// </summary>
        public bool RenderRunning;

        /// <summary>
        ///    Flag to determine if the emulator is running
        /// </summary>
        public bool EmulatorRunning;

        public SKBitmap EmulatorScreenBitmap { get; set; }

        /// <summary>
        ///     Byte Array containing the 8-bpp screen data
        /// </summary>
        private readonly byte[] _emulatorScreen = new byte[256 * 240];

        /// <summary>
        ///     Task that holds the rendering loop
        /// </summary>
        private readonly Task _renderTask;


        /// <summary>
        ///     Default Constructor
        /// </summary>
        public EmulatorPageViewModel()
        {
            BitmapRenderer = new SKBitmapRenderer(ADNES.Helpers.ColorHelper.ColorPalette);
            RenderRunning = true;

            _renderTask = Task.Factory.StartNew(Render);
        }

        [RelayCommand]
        public void ConsoleCanvas_OnTouch(SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    break;
                case SKTouchAction.Moved:
                    break;
                case SKTouchAction.Released:
                    break;
                case SKTouchAction.Cancelled:
                    break;
                case SKTouchAction.Entered:
                    return;
                case SKTouchAction.Exited:
                    break;
                case SKTouchAction.WheelChanged:
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        public void ControllerCanvas_OnTouch(SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    break;
                case SKTouchAction.Moved:
                    break;
                case SKTouchAction.Released:
                    break;
                case SKTouchAction.Cancelled:
                    break;
                case SKTouchAction.Entered:
                    return;
                case SKTouchAction.Exited:
                    break;
                case SKTouchAction.WheelChanged:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        ///    Rendering Loop for the Emulator
        ///
        ///     If ADNES is not running, render a noise bitmap to simulate television static
        /// </summary>
        public void Render()
        {
            while (RenderRunning)
            {
                //Send a message to the View to render the frame
                WeakReferenceMessenger.Default.Send(this);

                if (!EmulatorRunning)
                {
                    EmulatorScreenBitmap = BitmapRenderer.Render(BitmapRenderer.GenerateNoise(_emulatorScreen));
                    Task.Delay(33); //~29.97fps -- NTSC
                    continue;
                }
            }
        }

        /// <summary>
        ///     IDisposable Implementation
        /// </summary>
        public void Dispose()
        {
            //Gracefully Shut down the emulator
            EmulatorRunning = false;
            RenderRunning = false;
            _renderTask.Dispose();

            //Clean up the Bitmap
            EmulatorScreenBitmap.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
