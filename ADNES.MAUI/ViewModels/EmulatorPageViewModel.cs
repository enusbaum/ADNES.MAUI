using System.Collections.Concurrent;
using ADNES.Controller.Enums;
using ADNES.Enums;
using ADNES.MAUI.Extensions;
using ADNES.MAUI.Helpers;
using ADNES.MAUI.ViewModels.Enums;
using ADNES.MAUI.ViewModels.Messages;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SharpHook;
using SharpHook.Native;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace ADNES.MAUI.ViewModels
{
    public partial class EmulatorPageViewModel : ViewModelBase, IDisposable
    {
        /// <summary>
        ///     Bitmap Renderer used for converting the 8bpp bitmap data from ADNES to a 32bpp SKBitmap
        /// </summary>
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

        /// <summary>
        ///     Image Areas for the Controller Image on the View
        /// </summary>
        public LayeredImage ControllerImage { get; set; }

        /// <summary>
        ///     Image Areas for the Console Image on the View
        /// </summary>
        public LayeredImage ConsoleImage { get; set; }

        /// <summary>
        ///     Image Areas for the Emulator Image on the View
        /// </summary>
        public LayeredImage EmulatorImage { get; set; }

        /// <summary>
        ///     ADNES Emulator Instance
        /// </summary>
        private readonly Emulator _emulator;

        /// <summary>
        ///     Concurrent Queue to hold the frame data from ADNES
        /// </summary>
        private readonly ConcurrentQueue<byte[]> _frameDataBuffer = new();

        /// <summary>
        ///     Bitmap Renderer used for Rendering SKBitmaps for layers, messages, etc.
        /// </summary>
        private readonly SKBitmapRenderer _bitmapRenderer = new();

        /// <summary>
        ///     ID of the Pause Graphic Layer so it can be removed on unpause
        /// </summary>
        private Guid PauseGraphicId;

        /// <summary>
        ///     Dictionary we use to track the last time a control input animation was displayed as not to flood the view with animations
        /// </summary>
        private readonly ConcurrentDictionary<ControllerAreas, DateTime> _controllerAreaTouchAnimationTimes = new();

        /// <summary>
        ///     Default Constructor
        /// </summary>
        public EmulatorPageViewModel()
        {
            BitmapRenderer = new SKBitmapConverter(ADNES.Helpers.ColorHelper.ColorPalette);
            RenderRunning = true;

            //Setup ImageAreas for the Controller, Console, and Emulator Images to be used on the view
            ControllerImage = new LayeredImage("nes_controller.png", new Dictionary<int, SKRect>
                {
                    { (int)ControllerAreas.DPadUp, ControllerAreas.DPadUp.GetAttribute<AreaAttribute>()!.Rect},
                    { (int)ControllerAreas.DPadDown, ControllerAreas.DPadDown.GetAttribute<AreaAttribute>()!.Rect},
                    { (int)ControllerAreas.DPadLeft, ControllerAreas.DPadLeft.GetAttribute<AreaAttribute>()!.Rect},
                    { (int)ControllerAreas.DPadRight, ControllerAreas.DPadRight.GetAttribute<AreaAttribute>()!.Rect},
                    { (int)ControllerAreas.AButton, ControllerAreas.AButton.GetAttribute<AreaAttribute>()!.Rect},
                    { (int)ControllerAreas.BButton, ControllerAreas.BButton.GetAttribute<AreaAttribute>()!.Rect},
                    { (int)ControllerAreas.StartButton, ControllerAreas.StartButton.GetAttribute<AreaAttribute>()!.Rect},
                    { (int)ControllerAreas.SelectButton, ControllerAreas.SelectButton.GetAttribute<AreaAttribute>()!.Rect},
                }
                );
            ConsoleImage = new LayeredImage("nes_console.png", new Dictionary<int, SKRect>
            {
                {(int)ConsoleAreas.PowerLED, ConsoleAreas.PowerLED.GetAttribute<AreaAttribute>()!.Rect},
                {(int)ConsoleAreas.PowerButton, ConsoleAreas.PowerButton.GetAttribute<AreaAttribute>()!.Rect},
                {(int)ConsoleAreas.Cartridge, ConsoleAreas.Cartridge.GetAttribute<AreaAttribute>()!.Rect},
            });
            EmulatorImage = new LayeredImage("nes_static.png");

            //Initialize the ADNES Emulator
            _emulator = new Emulator(ProcessFrameFromADNES);

            //Start Render Task
            _renderTask = Task.Factory.StartNew(Render);
        }

        /// <summary>
        ///     Delegate method to process a frame from the ADNES emulator as they become ready
        /// </summary>
        /// <param name="frameData"></param>
        private void ProcessFrameFromADNES(byte[] frameData) => _frameDataBuffer.Enqueue(frameData);

        /// <summary>
        ///     Event Handler for the Console Canvas Touch Events
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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
                                    //Turn on the Red LED by adding a later to the Console Image
                                    //by drawing a solid red SKBitmap over the Power LED area
                                    var powerButton = ConsoleAreas.PowerLED.GetAttribute<AreaAttribute>()!.Rect;

                                    ConsoleImage.AddLayer(
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
                                if (await LoadROM())
                                {
                                    var messageBanner = EmulatorAreas.CenterBanner.GetAttribute<AreaAttribute>()!.Rect;

                                    EmulatorImage.AddLayer(
                                        _bitmapRenderer.RenderText(messageBanner.Size, "ROM LOADED", SKColors.Black, SKColors.White),
                                        messageBanner.Location, 2000);

                                    NotifyView(RedrawEvents.RedrawEmulator);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        /// <summary>
        ///    Event Handler for the Controller Canvas Touch Events
        /// </summary>
        /// <param name="e"></param>
        [RelayCommand]
        public void ControllerCanvas_OnTouch(SKTouchEventArgs e)
        {
            switch (e.ActionType)
            {
                //We'll see if the touch was in any ControllerAreas and send the appropriate button press
                case SKTouchAction.Pressed:
                    {
                        var inArea = (ControllerAreas)ControllerImage.InArea(e.Location);

                        switch (inArea)
                        {
                            case ControllerAreas.DPadUp:
                                _emulator.Controller1.ButtonPress(Buttons.Up);
                                break;
                            case ControllerAreas.DPadDown:
                                _emulator.Controller1.ButtonPress(Buttons.Down);
                                break;
                            case ControllerAreas.DPadLeft:
                                _emulator.Controller1.ButtonPress(Buttons.Left);
                                break;
                            case ControllerAreas.DPadRight:
                                _emulator.Controller1.ButtonPress(Buttons.Right);
                                break;
                            case ControllerAreas.AButton:
                                _emulator.Controller1.ButtonPress(Buttons.A);
                                break;
                            case ControllerAreas.BButton:
                                _emulator.Controller1.ButtonPress(Buttons.B);
                                break;
                            case ControllerAreas.StartButton:
                                _emulator.Controller1.ButtonPress(Buttons.Start);
                                break;
                            case ControllerAreas.SelectButton:
                                _emulator.Controller1.ButtonPress(Buttons.Select);
                                break;
                            default:
                                break;
                        }
                        break;
                    }

                //We'll see if any touch has released in any ControllerAreas and send the appropriate button release
                case SKTouchAction.Released:
                    {
                        var inArea = (ControllerAreas)ControllerImage.InArea(e.Location);

                        switch (inArea)
                        {
                            case ControllerAreas.DPadUp:
                                _emulator.Controller1.ButtonRelease(Buttons.Up);
                                break;
                            case ControllerAreas.DPadDown:
                                _emulator.Controller1.ButtonRelease(Buttons.Down);
                                break;
                            case ControllerAreas.DPadLeft:
                                _emulator.Controller1.ButtonRelease(Buttons.Left);
                                break;
                            case ControllerAreas.DPadRight:
                                _emulator.Controller1.ButtonRelease(Buttons.Right);
                                break;
                            case ControllerAreas.AButton:
                                _emulator.Controller1.ButtonRelease(Buttons.A);
                                break;
                            case ControllerAreas.BButton:
                                _emulator.Controller1.ButtonRelease(Buttons.B);
                                break;
                            case ControllerAreas.StartButton:
                                _emulator.Controller1.ButtonRelease(Buttons.Start);
                                break;
                            case ControllerAreas.SelectButton:
                                _emulator.Controller1.ButtonRelease(Buttons.Select);
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        /// <summary>
        ///     Renders a Touch Animation on the specified touch area for 750ms
        /// </summary>
        /// <param name="controllerArea"></param>
        private void RenderTouchAnimation(ControllerAreas controllerArea)
        {
            //Check to see if we've already displayed a touch animation in the last 750ms, if not, log this one -- otherwise bail
            if (_controllerAreaTouchAnimationTimes.TryGetValue(controllerArea, out var lastTouchTime))
            {
                if (DateTime.Now.Subtract(lastTouchTime).TotalMilliseconds < 250)
                    return;

                //Update the touch time in the dictionary since we're going to display a new one
                _controllerAreaTouchAnimationTimes.TryUpdate(controllerArea, DateTime.Now, lastTouchTime);
            }
            else
            {
                //Add the touch time to the dictionary (first touch)
                _controllerAreaTouchAnimationTimes.TryAdd(controllerArea, DateTime.Now);
            }

            var touchArea = controllerArea.GetAttribute<AreaAttribute>()!.Rect;

            //Find the center coordinates of the touch area
            var centerX = touchArea.Left + (touchArea.Width / 2);
            var centerY = touchArea.Top + (touchArea.Height / 2);

            var touchAnimationSize = new SKSize(75, 75);

            //Call the BitmapRenderer to render a touch animation
            var touchAnimation =
                _bitmapRenderer.RenderExpandingCircles(20, touchAnimationSize, SKColors.White, SKColors.Transparent);

            //Add the touch animation layers to the Controller Image, calculating how long each image should be delayed/displayed based on the number of images and the total time (750ms)
            var animationLayers = new List<Guid>();
            var displayTimePerFrame = 500 / touchAnimation.Count;
            for (var i = 0; i < touchAnimation.Count; i++)
            {
                animationLayers.Add(ControllerImage.AddLayer(touchAnimation[i], new SKPoint(centerX - (touchAnimationSize.Width / 2), centerY - (touchAnimationSize.Height / 2)), displayTimePerFrame, displayTimePerFrame * i));
            }

            //Kick off a task to notify the view to redraw the Controller Image 30 times a second and after 750ms remove the touch animation layers
            Task.Factory.StartNew(async () =>
            {
                for (var i = 0; i < 30; i++)
                {
                    NotifyView(RedrawEvents.RedrawController);
                    await Task.Delay(33);
                }
                ControllerImage.RemoveLayers(animationLayers);
                NotifyView(RedrawEvents.RedrawController);
            });

        }

        /// <summary>
        ///     Renders a layer over specified touch areas on the Controller to show what keyboard keys are mapped to what buttons
        ///
        ///     The help layers will be removed after 3 seconds
        /// </summary>
        private void ShowKeyboardHelp()
        {
            var helpLayers = new List<Guid>
            {
                //Render the Help Layers for the Keyboard Mappings
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.DPadUp.GetAttribute<AreaAttribute>()!.Rect.Size, "W", SKColors.Black, SKColors.White), ControllerAreas.DPadUp.GetAttribute<AreaAttribute>()!.Rect.Location, 3000),
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.DPadDown.GetAttribute<AreaAttribute>()!.Rect.Size, "S", SKColors.Black, SKColors.White), ControllerAreas.DPadDown.GetAttribute<AreaAttribute>()!.Rect.Location, 3000),
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.DPadLeft.GetAttribute<AreaAttribute>()!.Rect.Size, "A", SKColors.Black, SKColors.White), ControllerAreas.DPadLeft.GetAttribute<AreaAttribute>()!.Rect.Location, 3000),
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.DPadRight.GetAttribute<AreaAttribute>()!.Rect.Size, "D", SKColors.Black, SKColors.White), ControllerAreas.DPadRight.GetAttribute<AreaAttribute>()!.Rect.Location, 3000),
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.AButton.GetAttribute<AreaAttribute>()!.Rect.Size, "K", SKColors.Black, SKColors.White), ControllerAreas.AButton.GetAttribute<AreaAttribute>()!.Rect.Location, 3000),
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.BButton.GetAttribute<AreaAttribute>()!.Rect.Size, "L", SKColors.Black, SKColors.White), ControllerAreas.BButton.GetAttribute<AreaAttribute>()!.Rect.Location, 3000),
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.StartButton.GetAttribute<AreaAttribute>()!.Rect.Size, "Ent", SKColors.Black, SKColors.White), ControllerAreas.StartButton.GetAttribute<AreaAttribute>()!.Rect.Location, 3000),
                ControllerImage.AddLayer(_bitmapRenderer.RenderText(ControllerAreas.SelectButton.GetAttribute<AreaAttribute>()!.Rect.Size, "RS", SKColors.Black, SKColors.White), ControllerAreas.SelectButton.GetAttribute<AreaAttribute>()!.Rect.Location, 3000)
            };

            //Notify the view to redraw the Controller Image
            NotifyView(RedrawEvents.RedrawController);

            //Start a task to clean up the layers and notify the View to re-render the Controller Image without the help layers
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(3000);
                ControllerImage.RemoveLayers(helpLayers);
                NotifyView(RedrawEvents.RedrawController);
            });
        }

        /// <summary>
        ///     Handles input from the keyboard for Windows/Mac versions of the MAUI app
        /// </summary>
        public void Keyboard_OnKeyPress(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            //Handle F1 Key Press to show Keyboard Help regardless of emulator state
            if (keyboardHookEventArgs.Data.KeyCode == KeyCode.VcF1)
            {
                ShowKeyboardHelp();
                return;
            }

            //Ignore Key Presses if Emulator isn't running
            if (!_emulator.IsRunning || _emulator.State == EmulatorState.Paused)
                return;

            switch (keyboardHookEventArgs.Data.KeyCode)
            {
                case KeyCode.VcW:
                case KeyCode.VcUp:
                    _emulator.Controller1.ButtonPress(Buttons.Up);
                    RenderTouchAnimation(ControllerAreas.DPadUp);
                    break;
                case KeyCode.VcS:
                case KeyCode.VcDown:
                    _emulator.Controller1.ButtonPress(Buttons.Down);
                    RenderTouchAnimation(ControllerAreas.DPadDown);
                    break;
                case KeyCode.VcA:
                case KeyCode.VcLeft:
                    _emulator.Controller1.ButtonPress(Buttons.Left);
                    RenderTouchAnimation(ControllerAreas.DPadLeft);
                    break;
                case KeyCode.VcD:
                case KeyCode.VcRight:
                    _emulator.Controller1.ButtonPress(Buttons.Right);
                    RenderTouchAnimation(ControllerAreas.DPadRight);
                    break;
                case KeyCode.VcComma:
                    _emulator.Controller1.ButtonPress(Buttons.A);
                    RenderTouchAnimation(ControllerAreas.AButton);
                    break;
                case KeyCode.VcPeriod:
                    _emulator.Controller1.ButtonPress(Buttons.B);
                    RenderTouchAnimation(ControllerAreas.BButton);
                    break;
                case KeyCode.VcRightShift:
                    _emulator.Controller1.ButtonPress(Buttons.Select);
                    RenderTouchAnimation(ControllerAreas.SelectButton);
                    break;
                case KeyCode.VcEnter:
                    _emulator.Controller1.ButtonPress(Buttons.Start);
                    RenderTouchAnimation(ControllerAreas.StartButton);
                    break;
                default:
                    break;

            }
        }

        /// <summary>
        ///     Handles key release events for the keyboard
        /// </summary>
        public void Keyboard_OnKeyRelease(KeyboardHookEventArgs keyboardHookEventArgs)
        {
            //Ignore Key Releases if Emulator isn't running
            if (!_emulator.IsRunning || _emulator.State == EmulatorState.Paused)
                return;

            switch (keyboardHookEventArgs.Data.KeyCode)
            {
                case KeyCode.VcW:
                case KeyCode.VcUp:
                    _emulator.Controller1.ButtonRelease(Buttons.Up);
                    break;
                case KeyCode.VcS:
                case KeyCode.VcDown:
                    _emulator.Controller1.ButtonRelease(Buttons.Down);
                    break;
                case KeyCode.VcA:
                case KeyCode.VcLeft:
                    _emulator.Controller1.ButtonRelease(Buttons.Left);
                    break;
                case KeyCode.VcD:
                case KeyCode.VcRight:
                    _emulator.Controller1.ButtonRelease(Buttons.Right);
                    break;
                case KeyCode.VcComma:
                    _emulator.Controller1.ButtonRelease(Buttons.A);
                    break;
                case KeyCode.VcPeriod:
                    _emulator.Controller1.ButtonRelease(Buttons.B);
                    break;
                case KeyCode.VcRightShift:
                    _emulator.Controller1.ButtonRelease(Buttons.Select);
                    break;
                case KeyCode.VcEnter:
                    _emulator.Controller1.ButtonRelease(Buttons.Start);
                    break;
                default:
                    break;

            }
        }

        /// <summary>
        ///    Event Handler for the Emulator Canvas Touch Events
        /// </summary>
        /// <param name="e"></param>
        [RelayCommand]
        public void EmulatorCanvas_OnTouch(SKTouchEventArgs e)
        {
            //Only support touch events on the emulator canvas if it's running
            if (!_emulator.IsRunning)
                return;

            switch (e.ActionType)
            {
                case SKTouchAction.Pressed:
                    switch (_emulator.State)
                    {
                        case EmulatorState.Running:
                            {
                                _emulator.Pause();

                                var pauseGraphic = EmulatorAreas.PauseGraphic.GetAttribute<AreaAttribute>()!.Rect;

                                PauseGraphicId = EmulatorImage.AddLayer(
                                    _bitmapRenderer.RenderPauseGraphic(pauseGraphic.Size, SKColors.Black, SKColors.White), pauseGraphic.Location);

                                NotifyView(RedrawEvents.RedrawEmulator);
                                break;
                            }
                        case EmulatorState.Paused:
                            {
                                EmulatorImage.RemoveLayer(PauseGraphicId);

                                _emulator.Unpause();
                            }
                            break;
                    }
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
                //Render the frames as fast as possible without hogging the CPU
                Thread.Sleep(1);

                if (!EmulatorRunning)
                {
                    EmulatorImage.SetBaseImage(BitmapRenderer.CovertToBitmap(BitmapRenderer.GenerateNoise(_emulatorScreen)));
                    Thread.Sleep(33); //~29.97fps -- NTSC
                }
                else
                {
                    //If we're paused or for some reason the emulator Task is running but the state is stopped(?), wait
                    if (_emulator.State is EmulatorState.Paused or EmulatorState.Stopped)
                        continue;

                    if (_frameDataBuffer.TryDequeue(out var result))
                        EmulatorImage.SetBaseImage(BitmapRenderer.CovertToBitmap(result));
                }
                //Send a message to the View to render the frame
                NotifyView(RedrawEvents.RedrawEmulator);
            }
        }

        /// <summary>
        ///     Sends a notification to the view via the WeakReferenceMessenger to redraw the specified area
        ///     based on the RedrawEvent
        /// </summary>
        /// <param name="redrawEvent"></param>
        private void NotifyView(RedrawEvents redrawEvent)
        {
            WeakReferenceMessenger.Default.Send(new EventMessage() { RedrawEvent = redrawEvent });
        }

        /// <summary>
        ///     Presents a file picker for the user to select which NES ROM to load
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoadROM()
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
                return true;
            }

            return false;
        }

        /// <summary>
        ///     IDisposable Implementation
        /// </summary>
        public void Dispose()
        {
            //Gracefully Shut down the emulator
            _emulator.Stop();
            RenderRunning = false;

            //Wait for _renderTask to complete then dispose of it
            _renderTask.Wait();
            _renderTask.Dispose();

            //Clean up the Bitmap
            EmulatorScreenBitmap?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
