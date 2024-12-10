using ADNES.MAUI.Helpers;
using ADNES.MAUI.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Runtime.CompilerServices;

namespace ADNES.MAUI.Pages
{
    public partial class EmulatorPage : ContentPage, IRecipient<EmulatorPageViewModel>
    {
        private readonly SkiaHelpers _skiaHelpers;
        private SKBitmap _controllerBitmap;
        private SKBitmap _consoleBitmap;
        private SKBitmap _emulatorBitmap;

        public EmulatorPage(SkiaHelpers skiaHelpers)
        {
            InitializeComponent();
            _skiaHelpers = skiaHelpers;
        }

        /// <summary>
        ///     Event handler for when the page is appearing. This is used to load the initial images into SKBitmaps and subscribe to events.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            //Load initial images into SKBitmaps
            _controllerBitmap = await _skiaHelpers.GetSKBitmapFromResource("nes_controller.png");
            _consoleBitmap = await _skiaHelpers.GetSKBitmapFromResource("nes_console.png");
            _emulatorBitmap = await _skiaHelpers.GetSKBitmapFromResource("nes_static.png");

            //Subscribe to events to draw the bitmaps on the canvas from the ViewModel
            WeakReferenceMessenger.Default.Register(this);

            //Add event to Unloaded to safely dispose of the ViewModel
            Unloaded += (sender, e) =>
            {
                (((ContentPage)sender)?.BindingContext as IDisposable)?.Dispose();
            };
        }

        /// <summary>
        ///     Event handler for when the canvas size changes. This is used to ensure the SKCanvasView is sized correctly
        ///     depending on the aspect ratio of the image being drawn and the current width of the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void OnCanvasSizeChanged(object sender, EventArgs e)
        {
            var canvasReference = (SKCanvasView)sender;

            //Don't invoke if the view hasn't loaded yet
            if (canvasReference.Width <= 0) return;

            var bitmapToLoad = canvasReference.StyleId switch //Determine which Canvas is raising the event and load the associated bitmap
            {
                "ConsoleCanvas" => _consoleBitmap,
                "EmulatorCanvas" => _emulatorBitmap,
                "ControllerCanvas" => _controllerBitmap,
                _ => throw new ArgumentOutOfRangeException()
            };

            var aspectRatio = (float)bitmapToLoad.Height / bitmapToLoad.Width;
            var desiredHeight = (float)canvasReference.Width * aspectRatio;

            canvasReference.HeightRequest = desiredHeight;
;
            canvasReference.InvalidateSurface();
        }

        /// <summary>
        ///     Event handler for when the canvas is painted. This is used to draw the bitmap on the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void OnCanvasPaint(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvasReference = (SKCanvasView)sender;
            var bitmapToLoad = canvasReference.StyleId switch //Determine which Canvas is raising the event and load the associated bitmap
            {
                "ConsoleCanvas" => _consoleBitmap,
                "ControllerCanvas" => _controllerBitmap,
                _ => throw new ArgumentOutOfRangeException()
            };
            DrawBitmapOnCanvas(bitmapToLoad, e.Surface.Canvas, e.Info.Height, e.Info.Width);
            canvasReference.PaintSurface -= OnCanvasPaint; //Unsubscribe from the event as updates from here on in will come from the ViewModel
        }

        /// <summary>
        ///     Event handler for when the emulator canvas is painted. This is used to draw the emulator screen bitmap on the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEmulatorCanvasPaint(object sender, SKPaintSurfaceEventArgs e)
        {
            var viewModel = (EmulatorPageViewModel)BindingContext;
            DrawBitmapOnCanvas(
                (viewModel.RenderRunning ? viewModel.EmulatorScreenBitmap : _emulatorBitmap), 
                e.Surface.Canvas, 
                e.Info.Height, 
                e.Info.Width
                );
        }

        /// <summary>
        ///     General method to draw a bitmap on the canvas, scaled to fit the canvas.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="canvas"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void DrawBitmapOnCanvas(SKBitmap bitmap, SKCanvas canvas, float height, float width)
        {
            // Draw the bitmap scaled to fit the canvas (now the canvas should be sized appropriately)
            var scale = Math.Min(width / bitmap.Width, height / bitmap.Height);
            var newWidth = bitmap.Width * scale;
            var newHeight = bitmap.Height * scale;
            var left = (width - newWidth) / 2f;
            var top = (height - newHeight) / 2f;

            var destRect = new SKRect(left, top, left + newWidth, top + newHeight);
            canvas.DrawBitmap(bitmap, destRect);
        }

        /// <summary>
        ///     Method is invoked when the ViewModel sends notification that a new frame is ready to be rendered
        /// </summary>
        /// <param name="message"></param>
        public void Receive(EmulatorPageViewModel message)
        {
            EmulatorCanvas.InvalidateSurface();
        }
    }
}