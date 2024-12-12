using System.Collections.Concurrent;
using ADNES.MAUI.Extensions;
using ADNES.MAUI.Helpers;
using ADNES.MAUI.ViewModels.Enums;
using ADNES.MAUI.ViewModels.Messages;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace ADNES.MAUI.ViewModels
{
    public partial class EmulatorPageViewModel : ViewModelBase, IDisposable
    {
        public readonly SKBitmapConverter BitmapRenderer;

        /// <summary>
        ///     Flag to determine if the rendering loop is running
        /// </summary>
        public bool RenderRunning;

        /// <summary>
        ///    Flag to determine if the emulator is running
        /// </summary>
        public bool EmulatorRunning => _emulator.IsRunning;

        public SKBitmap EmulatorScreenBitmap { get; set; }

        /// <summary>
        ///     Byte Array containing the 8-bpp screen data
        /// </summary>
        private readonly byte[] _emulatorScreen = new byte[256 * 240];

        /// <summary>
        ///     Task that holds the rendering loop
        /// </summary>
        private readonly Task _renderTask;

        public ImageArea ControllerImage { get; set; }
        public ImageArea ConsoleImage { get; set; }
        public ImageArea EmulatorImage { get; set; }

        private readonly Emulator _emulator;

        private readonly ConcurrentQueue<byte[]> _frameDataBuffer = new();

        private readonly SKBitmapRenderer _bitmapRenderer = new();

        public EmulatorPageViewModel()
        {
            BitmapRenderer = new SKBitmapConverter(ADNES.Helpers.ColorHelper.ColorPalette);
            RenderRunning = true;

            ControllerImage = new ImageArea("nes_controller.png");
            ConsoleImage = new ImageArea("nes_console.png", new Dictionary<int, SKRect>()
            {
                {(int)ConsoleAreas.PowerLED, ConsoleAreas.PowerLED.GetAttribute<AreaAttribute>()!.Rect},
                {(int)ConsoleAreas.PowerButton, ConsoleAreas.PowerButton.GetAttribute<AreaAttribute>()!.Rect},
                {(int)ConsoleAreas.Cartridge, ConsoleAreas.Cartridge.GetAttribute<AreaAttribute>()!.Rect},
            });
            EmulatorImage = new ImageArea("nes_static.png");

            _emulator = new Emulator(ProcessFrameFromADNES);

            _renderTask = Task.Factory.StartNew(Render);
        }

        /// <summary>
        ///     Delegate method to process a frame from the ADNES emulator as they become ready
        /// </summary>
        /// <param name="frameData"></param>
        private void ProcessFrameFromADNES(byte[] frameData) => _frameDataBuffer.Enqueue(frameData);

        [RelayCommand]
        public async Task ConsoleCanvas_OnTouch(SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    {
                        var inArea = ConsoleImage.InArea(e.Location);

                        //Not in an area
                        if (inArea == -1)
                            return;

                        switch ((ConsoleAreas)inArea)
                        {
                            case ConsoleAreas.PowerLED:
                                break;
                            case ConsoleAreas.PowerButton:
                                if (!_emulator.IsRunning)
                                {
                                    //Turn on the Red LED by adding an overlay to the Console Image
                                    //by drawing a solid red SKBitmap over the Power LED area
                                    var powerButton =ConsoleAreas.PowerLED.GetAttribute<AreaAttribute>()!.Rect;
                                    ConsoleImage.AddOverlay(
                                        _bitmapRenderer.RenderSolidColor(
                                            powerButton.Size, SKColors.Red), powerButton.Location);
                                    
                                    _emulator.Start();

                                    NotifyView(RedrawEvents.RedrawConsole);
                                }
                                else
                                {
                                    _emulator.Stop();
                                }
                                break;
                            case ConsoleAreas.ResetButton:
                                break;
                            case ConsoleAreas.Cartridge:
                                await LoadROM();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
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
                if (!EmulatorRunning)
                {
                    EmulatorScreenBitmap = BitmapRenderer.Render(BitmapRenderer.GenerateNoise(_emulatorScreen));
                }
                else
                {
                    if (_frameDataBuffer.TryDequeue(out var result))
                        EmulatorScreenBitmap = BitmapRenderer.Render(result);
                }
                //Send a message to the View to render the frame
                NotifyView(RedrawEvents.RedrawEmulator);

                Task.Delay(33); //~29.97fps -- NTSC
            }
        }

        private void NotifyView(RedrawEvents redrawEvent)
        {
            WeakReferenceMessenger.Default.Send(new EventMessage() { RedrawEvent = redrawEvent });
        }

        /// <summary>
        ///     Presents a file picker for the user to select which NES ROM to load
        /// </summary>
        /// <returns></returns>
        public async Task LoadROM()
        {
            var options = new PickOptions()
            {
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        // For iOS/macOS: a generic UTI that will at least allow binary files.
                        { DevicePlatform.iOS, ["public.data"] },
                        { DevicePlatform.macOS, ["public.data"] },

                        // For Android: a generic binary MIME type.
                        { DevicePlatform.Android, ["application/octet-stream"] },

                        // For WinUI: the actual file extension.
                        { DevicePlatform.WinUI, [".nes"] },

                        // For Tizen: either a generic binary MIME type or a catch-all.
                        { DevicePlatform.Tizen, ["application/octet-stream"] }
                    }),
                PickerTitle = "Load NES ROM..."
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                //Load the selected ROM into ADNES
                await using var stream = await result.OpenReadAsync();
                var buffer = new byte[stream.Length];
                await stream.ReadExactlyAsync(buffer.AsMemory(0, (int)stream.Length));
                _emulator.LoadRom(buffer);

            }

        }

        /// <summary>
        ///     IDisposable Implementation
        /// </summary>
        public void Dispose()
        {
            //Gracefully Shut down the emulator
            _emulator.Stop();
            RenderRunning = false;
            _renderTask.Dispose();

            //Clean up the Bitmap
            EmulatorScreenBitmap.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
