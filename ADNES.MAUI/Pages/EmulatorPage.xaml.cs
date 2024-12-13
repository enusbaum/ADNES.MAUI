using ADNES.MAUI.Helpers;
using ADNES.MAUI.ViewModels;
using ADNES.MAUI.ViewModels.Enums;
using ADNES.MAUI.ViewModels.Messages;
using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Runtime.CompilerServices;

namespace ADNES.MAUI.Pages
{
    public partial class EmulatorPage : ContentPage, IRecipient<EventMessage>
    {

        public EmulatorPage()
        {
            InitializeComponent();

            // Initial state: very thin horizontally, visible line
            EmulatorCanvas.ScaleY = 0.01;
            EmulatorCanvas.AnchorY = 0.5; // Expand from center vertically
            EmulatorCanvas.Opacity = 0;
        }

        /// <summary>
        ///     Event handler for when the page is appearing. This is used to load the initial images into SKBitmaps and subscribe to events.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            //Subscribe to events to draw the bitmaps on the canvas from the ViewModel
            WeakReferenceMessenger.Default.Register(this);

            //Add event to Unloaded to safely dispose of the ViewModel
            Unloaded += (sender, e) =>
            {
                (((ContentPage)sender)?.BindingContext as IDisposable)?.Dispose();
            };

            // Fade in and then expand vertically
            EmulatorCanvas.FadeTo(1, 1000, Easing.Linear).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EmulatorCanvas.ScaleYTo(1, 2000, Easing.CubicOut);
                });
            });

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
            var viewModel = (EmulatorPageViewModel)BindingContext;

            var imageArea = GetImageAreaByStyleId(canvasReference.StyleId);

            var aspectRatio = (float)imageArea.Image.Height / imageArea.Image.Width;
            var desiredHeight = (float)canvasReference.Width * aspectRatio;
            canvasReference.HeightRequest = desiredHeight;

            imageArea.CalculateAreas(new SKSize((float)canvasReference.Width, desiredHeight));

            canvasReference.InvalidateSurface();
        }

        /// <summary>
        ///     Event handler for when the canvas is painted. This is used to draw the bitmap on the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void OnCanvasPaint(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvasReference = sender as SKCanvasView;
            var viewModel = (EmulatorPageViewModel)BindingContext;

            var imageArea = GetImageAreaByStyleId(canvasReference?.StyleId);

            DrawBitmapOnCanvas(imageArea.Image, e.Surface.Canvas, e.Info.Height, e.Info.Width);

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
                (viewModel.RenderRunning ? viewModel.EmulatorScreenBitmap : viewModel.EmulatorImage.Image),
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
        /// <param name="eventMessage"></param>
        public void Receive(EventMessage eventMessage)
        {
            switch(eventMessage.RedrawEvent)
            {
                case RedrawEvents.RedrawEmulator:
                    EmulatorCanvas.InvalidateSurface();
                    break;
                case RedrawEvents.RedrawConsole:
                    ConsoleCanvas.PaintSurface += OnCanvasPaint;
                    ConsoleCanvas.InvalidateSurface();
                    break;
                case RedrawEvents.RedrawController:
                    ControllerCanvas.PaintSurface += OnCanvasPaint;
                    ControllerCanvas.InvalidateSurface();
                    break;
            }
        }

        /// <summary>
        ///     Gets the ImageArea from the ViewModel based on the StyleId of the canvas
        /// </summary>
        /// <param name="styleId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private ImageArea GetImageAreaByStyleId(string styleId) =>
            styleId switch //Determine which Canvas is raising the event and load the associated bitmap
            {
                "ConsoleCanvas" => ((EmulatorPageViewModel)BindingContext).ConsoleImage,
                "EmulatorCanvas" => ((EmulatorPageViewModel)BindingContext).EmulatorImage,
                "ControllerCanvas" => ((EmulatorPageViewModel)BindingContext).ControllerImage,
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}