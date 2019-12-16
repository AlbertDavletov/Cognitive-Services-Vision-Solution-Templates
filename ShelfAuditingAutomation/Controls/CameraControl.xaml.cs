using ShelfAuditingAutomation.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.System.Threading;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ShelfAuditingAutomation.Controls
{
    public sealed partial class CameraControl : UserControl
    {
        public bool PerformFaceTracking { get; set; } = true;

        public event EventHandler<ImageAnalyzer> ImageCaptured;
        public event EventHandler CameraRestarted;
        public event EventHandler CameraAspectRatioChanged;

        public double CameraAspectRatio { get; set; }
        public int CameraResolutionWidth { get; private set; }
        public int CameraResolutionHeight { get; private set; }

        public int NumFacesOnLastFrame { get; set; }
        public int ContinuousModeTimerInSecond { get; set; } = 5;

        public CameraStreamState CameraStreamState { get { return captureManager != null ? captureManager.CameraStreamState : CameraStreamState.NotStreaming; } }

        private MediaCapture captureManager;
        private ThreadPoolTimer frameProcessingTimer;
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);
        private bool isStreamingOnRealtimeResolution = false;
        private DeviceInformation lastUsedCamera;


        public CameraControl()
        {
            this.InitializeComponent();

            Window.Current.Activated += CurrentWindowActivationStateChanged;
        }

        private async void CurrentWindowActivationStateChanged(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if ((e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated ||
                e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.PointerActivated) &&
                captureManager?.CameraStreamState == CameraStreamState.Shutdown &&
                webCamCaptureElement.Visibility == Visibility.Visible)
            {
                // When an app is running full screen and it loses focus due to user interaction, Windows will shut the camera down.
                // We detect here when the window regains focus and trigger a restart of the camera, but only if detect the camera was supposed to 
                // be visible and its state is actually Shutdown.
                await StartStreamAsync(isForRealTimeProcessing: isStreamingOnRealtimeResolution, desiredCamera: lastUsedCamera);
            }
        }

        #region Camera stream processing

        public async Task StartStreamAsync(bool isForRealTimeProcessing = false, DeviceInformation desiredCamera = null)
        {
            try
            {
                if (captureManager == null ||
                    captureManager.CameraStreamState == CameraStreamState.Shutdown ||
                    captureManager.CameraStreamState == CameraStreamState.NotStreaming)
                {
                    loadingOverlay.Visibility = Visibility.Visible;

                    if (captureManager != null)
                    {
                        captureManager.Dispose();
                    }

                    captureManager = new MediaCapture();

                    MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                    var allCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                    var selectedCamera = allCameras.FirstOrDefault();
                    if (desiredCamera != null)
                    {
                        selectedCamera = desiredCamera;
                    }
                    else if (lastUsedCamera != null)
                    {
                        selectedCamera = lastUsedCamera;
                    }

                    if (selectedCamera != null)
                    {
                        settings.VideoDeviceId = selectedCamera.Id;
                        lastUsedCamera = selectedCamera;
                    }

                    cameraSwitchButton.Visibility = allCameras.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

                    await captureManager.InitializeAsync(settings);

                    SetZoomSlider();
                    await SetVideoEncodingToHighestResolution(isForRealTimeProcessing);
                    isStreamingOnRealtimeResolution = isForRealTimeProcessing;

                    this.webCamCaptureElement.Source = captureManager;
                }

                if (captureManager.CameraStreamState == CameraStreamState.NotStreaming)
                {
                    await captureManager.StartPreviewAsync();

                    this.webCamCaptureElement.Visibility = Visibility.Visible;

                    loadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "Error starting the camera.").ShowAsync();
            }
        }

        private async Task SetVideoEncodingToHighestResolution(bool isForRealTimeProcessing = false)
        {
            VideoEncodingProperties highestVideoEncodingSetting;

            // Sort the available resolutions from highest to lowest
            var availableResolutions = this.captureManager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Cast<VideoEncodingProperties>().OrderByDescending(v => v.Width * v.Height * (v.FrameRate.Numerator / v.FrameRate.Denominator));

            if (isForRealTimeProcessing)
            {
                uint maxHeightForRealTime = 720;
                // Find the highest resolution that is 720p or lower
                highestVideoEncodingSetting = availableResolutions.FirstOrDefault(v => v.Height <= maxHeightForRealTime);
                if (highestVideoEncodingSetting == null)
                {
                    // Since we didn't find 720p or lower, look for the first up from there
                    highestVideoEncodingSetting = availableResolutions.LastOrDefault();
                }
            }
            else
            {
                // Use the highest resolution
                highestVideoEncodingSetting = availableResolutions.FirstOrDefault();
            }

            if (highestVideoEncodingSetting != null)
            {
                this.CameraAspectRatio = (double)highestVideoEncodingSetting.Width / (double)highestVideoEncodingSetting.Height;
                this.CameraResolutionHeight = (int)highestVideoEncodingSetting.Height;
                this.CameraResolutionWidth = (int)highestVideoEncodingSetting.Width;

                await this.captureManager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, highestVideoEncodingSetting);

                this.CameraAspectRatioChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetZoomSlider()
        {
            ZoomControl zoomControl = this.captureManager.VideoDeviceController.ZoomControl;
            if (zoomControl != null && zoomControl.Supported)
            {
                zoomGrid.Visibility = Visibility.Visible;

                zoomSlider.Minimum = zoomControl.Min;
                zoomSlider.Maximum = zoomControl.Max;
                zoomSlider.StepFrequency = zoomControl.Step;

                zoomSlider.Value = zoomControl.Value;
            }
            else
            {
                zoomGrid.Visibility = Visibility.Collapsed;
            }
        }

        public async Task StopStreamAsync()
        {
            try
            {
                if (this.frameProcessingTimer != null)
                {
                    this.frameProcessingTimer.Cancel();
                }

                if (captureManager != null && captureManager.CameraStreamState != Windows.Media.Devices.CameraStreamState.Shutdown)
                {
                    await this.captureManager.StopPreviewAsync();

                    this.webCamCaptureElement.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
            }
        }

        public async Task<ImageAnalyzer> CaptureFrameAsync()
        {
            try
            {
                if (!(await this.frameProcessingSemaphore.WaitAsync(250)))
                {
                    return null;
                }

                // Capture a frame from the preview stream
                var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, CameraResolutionWidth, CameraResolutionHeight);
                using (var currentFrame = await captureManager.GetPreviewFrameAsync(videoFrame))
                {
                    using (SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap)
                    {
                        ImageAnalyzer imageWithFace = new ImageAnalyzer(await Util.GetPixelBytesFromSoftwareBitmapAsync(previewFrame));

                        imageWithFace.UpdateDecodedImageSize(this.CameraResolutionHeight, this.CameraResolutionWidth);

                        return imageWithFace;
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                this.frameProcessingSemaphore.Release();
            }

            return null;
        }

        private void OnImageCaptured(ImageAnalyzer imageWithFace)
        {
            this.ImageCaptured?.Invoke(this, imageWithFace);
        }
        #endregion


        private async void CameraControlButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.captureManager.CameraStreamState == CameraStreamState.Streaming)
            {
                var img = await CaptureFrameAsync();
                if (img != null)
                {
                    this.OnImageCaptured(img);
                }
            }
            else
            {
                await StartStreamAsync(isStreamingOnRealtimeResolution, lastUsedCamera);

                this.CameraRestarted?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void CameraSwitchtButtonClick(object sender, RoutedEventArgs e)
        {
            // if we are not streaming just ignore the request
            if (captureManager.CameraStreamState != CameraStreamState.Streaming)
            {
                return;
            }

            // capture current device id
            string currentCameraId = captureManager.MediaCaptureSettings.VideoDeviceId;

            // stop camera
            await StopStreamAsync();

            // start streaming with the camera whose index is the next one in the line
            var allCameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            int currentCameraIndex = allCameras.ToList().FindIndex(d => string.Compare(d.Id, currentCameraId, ignoreCase: true) == 0);
            await StartStreamAsync(isStreamingOnRealtimeResolution, allCameras.ElementAt((currentCameraIndex + 1) % allCameras.Count));
        }

        private async void ControlUnloaded(object sender, RoutedEventArgs e)
        {
            await StopStreamAsync();
            Window.Current.Activated -= CurrentWindowActivationStateChanged;
        }

        private void OnZoomSliderValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var level = (float)zoomSlider.Value;
            var settings = new ZoomSettings { Value = level };

            var zoomControl = this.captureManager.VideoDeviceController.ZoomControl;
            if (zoomControl.SupportedModes.Contains(ZoomTransitionMode.Smooth))
            {
                settings.Mode = ZoomTransitionMode.Smooth;
            }
            else
            {
                settings.Mode = zoomControl.SupportedModes.First();
            }

            zoomControl.Configure(settings);
        }

        private void OnZoomButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button zoomButton)
            {
                string tag = (string)zoomButton.Tag;
                switch (tag)
                {
                    case "ZoomOut":
                        this.zoomSlider.Value -= 1;
                        break;
                    case "ZoomIn":
                        this.zoomSlider.Value += 1;
                        break;
                }
            }
        }
    }
}
