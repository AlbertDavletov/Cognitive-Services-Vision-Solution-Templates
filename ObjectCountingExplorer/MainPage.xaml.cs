using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Toolkit.Uwp.UI.Controls;
using ObjectCountingExplorer.Controls;
using ObjectCountingExplorer.Helpers;
using ObjectCountingExplorer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace ObjectCountingExplorer
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private static readonly string TrainingApiKey = "<CUSTOM VISION TRANING API KEY>";
        private static readonly string TrainingApiKeyEndpoint = "<CUSTOM VISION TRANING API ENDPOINT>";
        private static readonly string PredictionApiKey = "<CUSTOM VISION PREDICTION API KEY>";
        private static readonly string PredictionApiKeyEndpoint = "<CUSTOM VISION PREDICTION API ENDPOINT>";
        private static readonly ProjectViewModel currentProject = new ProjectViewModel(Guid.Empty, "<MODEL NAME>");

        private bool enableEditMode = false;
        private IEnumerable<PredictionModel> currentDetectedObjects;
        private StorageFile currentImageFile;
        private CustomVisionTrainingClient trainingApi;
        private CustomVisionPredictionClient predictionApi;

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<RecognitionGroupViewModel> RecognitionGroupCollection { get; set; } = new ObservableCollection<RecognitionGroupViewModel>
        {
            new RecognitionGroupViewModel { Name = "Summary", Group = RecognitionGroup.Summary },
            new RecognitionGroupViewModel { Name = "Low Confidence", Group = RecognitionGroup.LowConfidence },
            new RecognitionGroupViewModel { Name = "Medium Confidence", Group = RecognitionGroup.MediumConfidence },
            new RecognitionGroupViewModel { Name = "High Confidence", Group = RecognitionGroup.HighConfidence }
        };

        public ObservableCollection<ProductItemViewModel> LowConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> MediumConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> HighConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<DetectedObjectsViewModel> DetectedObjectCollection { get; set; } = new ObservableCollection<DetectedObjectsViewModel>();

        public bool enableCropFeature = false;
        public bool EnableCropFeature
        {
            get { return enableCropFeature; }
            set
            {
                enableCropFeature = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EnableCropFeature"));

                ImageViewChanged();
            }
        }

        private RecognitionGroup recognitionGroup;
        public RecognitionGroup RecognitionGroup
        {
            get { return recognitionGroup; }
            set
            {
                recognitionGroup = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecognitionGroup"));
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!string.IsNullOrEmpty(TrainingApiKey) && !string.IsNullOrEmpty(TrainingApiKeyEndpoint) &&
                !string.IsNullOrEmpty(PredictionApiKey) && !string.IsNullOrEmpty(PredictionApiKeyEndpoint))
            {
                this.mainPage.IsEnabled = true;
                trainingApi = new CustomVisionTrainingClient { Endpoint = TrainingApiKeyEndpoint, ApiKey = TrainingApiKey };
                predictionApi = new CustomVisionPredictionClient { Endpoint = PredictionApiKeyEndpoint, ApiKey = PredictionApiKey };

                await LoadTagColorAsync();
            }
            else
            {
                this.mainPage.IsEnabled = true;
                await new MessageDialog("Please enter Custom Vision API Keys in the code behind of this demo.", "Missing API Keys").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async void OnWebCamButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
            {
                await StopWebCameraAsync();
            }
            else
            {
                await StartWebCameraAsync();
            }
        }

        private async Task StartWebCameraAsync()
        {
            this.initialView.Visibility = Visibility.Collapsed;

            this.imageGrid.Visibility = Visibility.Collapsed;
            this.webCamHostGrid.Visibility = Visibility.Visible;

            await this.cameraControl.StartStreamAsync();
            await Task.Delay(250);

            UpdateWebCamHostGridSize();
        }

        private async Task StopWebCameraAsync()
        {
            this.initialView.Visibility = Visibility.Visible;

            await this.cameraControl.StopStreamAsync();
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWebCamHostGridSize();
        }

        private void UpdateWebCamHostGridSize()
        {
            this.webCamHostGrid.Width = this.webCamHostGrid.ActualHeight * (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }

        private async void ImageViewChanged()
        {
            if (EnableCropFeature)
            {
                await this.imageCropper.LoadImageFromFile(currentImageFile);
            }
            else
            {
                if (currentImageFile != null)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync((await currentImageFile.OpenStreamForReadAsync()).AsRandomAccessStream());
                    this.image.Source = bitmapImage;
                }
            }
        }

        private async void CameraControl_ImageCaptured(object sender, ImageAnalyzer img)
        {
            ResetImageData();

            currentImageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Image.jpg", CreationCollisionOption.ReplaceExisting);

            if (img.ImageUrl != null)
            {
                await Util.DownloadAndSaveBitmapAsync(img.ImageUrl, currentImageFile);
            }
            else if (img.GetImageStreamCallback != null)
            {
                await Util.SaveBitmapToStorageFileAsync(await img.GetImageStreamCallback(), currentImageFile);
            }

            BitmapImage bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync((await currentImageFile.OpenStreamForReadAsync()).AsRandomAccessStream());
            this.image.Source = bitmapImage;

            this.UpdateActivePhoto();
        }

        private async void OnBrowseImageButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary, ViewMode = PickerViewMode.Thumbnail };
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".jpeg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.FileTypeFilter.Add(".bmp");
                StorageFile selectedFile = await fileOpenPicker.PickSingleFileAsync();

                if (selectedFile != null)
                {
                    ResetImageData();
                    currentImageFile = await selectedFile.CopyAsync(ApplicationData.Current.LocalFolder, "Image.jpg", NameCollisionOption.ReplaceExisting);

                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync((await currentImageFile.OpenStreamForReadAsync()).AsRandomAccessStream());
                    this.image.Source = bitmapImage;

                    this.UpdateActivePhoto();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "Open file error").ShowAsync();
            }
        }

        private async void ResetImageData()
        {
            this.enableEditMode = false;
            this.image.Source = null;
            this.currentDetectedObjects = null;
            this.objectDetectionVisualizationCanvas.Children.Clear();

            DetectedObjectCollection.Clear();
            LowConfidenceCollection.Clear();
            MediumConfidenceCollection.Clear();
            HighConfidenceCollection.Clear();

            this.recognitionGroupListView.SelectedIndex = -1;
            this.chartControl.Visibility = Visibility.Collapsed;

            if (this.cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
            {
                await StopWebCameraAsync();
            }
        }

        private void UpdateActivePhoto()
        {
            this.imageGrid.Visibility = Visibility.Visible;
            this.analyzeButton.Visibility = Visibility.Visible;

            this.webCamHostGrid.Visibility = Visibility.Collapsed;
            this.initialView.Visibility = Visibility.Collapsed;
        }

        private async void OnCloseImageViewButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
            {
                await StopWebCameraAsync();
            }
            this.imageCropper.Source = null;
            this.initialView.Visibility = Visibility.Visible;
            this.analyzeButton.Visibility = Visibility.Collapsed;
        }

        private void OnCancelCropImageButtonClicked(object sender, RoutedEventArgs e)
        {
            this.imageCropper.Reset();
            EnableCropFeature = false;
        }

        private async void OnSaveImageButtonClicked(object sender, RoutedEventArgs e)
        {
            await SaveImageToFileAsync(currentImageFile);
            EnableCropFeature = false;
        }

        private async void OnAnalyzeImageButtonClicked(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                var productItemCollection = new List<ProductItemViewModel>()
                {
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.99,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.98,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.97,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.96,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.95,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.94,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.6,   tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.59,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.58,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.3,  tagName: "General Mills") },
                    new ProductItemViewModel() { Name = "General Mills", Model = new PredictionModel(probability: 0.2,  tagName: "General Mills") },


                    new ProductItemViewModel() { Name = "Great Value", Model = new PredictionModel(probability: 0.81,  tagName: "Great Value") },
                    new ProductItemViewModel() { Name = "Great Value", Model = new PredictionModel(probability: 0.79,  tagName: "Great Value") },
                    new ProductItemViewModel() { Name = "Great Value", Model = new PredictionModel(probability: 0.78,  tagName: "Great Value") },
                    new ProductItemViewModel() { Name = "Great Value", Model = new PredictionModel(probability: 0.59,  tagName: "Great Value") },
                    new ProductItemViewModel() { Name = "Great Value", Model = new PredictionModel(probability: 0.58,  tagName: "Great Value") },


                    new ProductItemViewModel() { Name = "Quaker", Model = new PredictionModel(probability: 0.99, tagName: "Quaker") },
                    new ProductItemViewModel() { Name = "Quaker", Model = new PredictionModel(probability: 0.98, tagName: "Quaker") },
                    new ProductItemViewModel() { Name = "Quaker", Model = new PredictionModel(probability: 0.97, tagName: "Quaker") },
                    new ProductItemViewModel() { Name = "Quaker", Model = new PredictionModel(probability: 0.96, tagName: "Quaker") },


                    new ProductItemViewModel() { Name = "Kellog", Model = new PredictionModel(probability: 0.8,  tagName: "Kellog") },
                    new ProductItemViewModel() { Name = "Kellog", Model = new PredictionModel(probability: 0.81, tagName: "Kellog") },
                    new ProductItemViewModel() { Name = "Kellog", Model = new PredictionModel(probability: 0.82, tagName: "Kellog") },
                    new ProductItemViewModel() { Name = "Kellog", Model = new PredictionModel(probability: 0.7,  tagName: "Kellog") },


                    new ProductItemViewModel() { Name = "None", Model = new PredictionModel(probability: 0.8,  tagName: "None") },
                    new ProductItemViewModel() { Name = "None", Model = new PredictionModel(probability: 0.81, tagName: "None") },
                    new ProductItemViewModel() { Name = "None", Model = new PredictionModel(probability: 0.82, tagName: "None") }
                };

                LowConfidenceCollection.Clear();
                LowConfidenceCollection.AddRange(productItemCollection.Where(x => x.Model.Probability <= 0.3));

                MediumConfidenceCollection.Clear();
                MediumConfidenceCollection.AddRange(productItemCollection.Where(x => x.Model.Probability > 0.3 && x.Model.Probability <= 0.6));

                HighConfidenceCollection.Clear();
                HighConfidenceCollection.AddRange(productItemCollection.Where(x => x.Model.Probability > 0.6));

                this.recognitionGroupListView.SelectedIndex = 0;
                this.chartControl.Visibility = Visibility.Visible;
                this.chartControl.UpdateChart(productItemCollection);

                //DetectedObjectCollection.Clear();

                //ImagePrediction result = await AnalyzeImageAsync(currentProject, currentImageFile);
                //
                //currentDetectedObjects = result?.Predictions?.Where(p => p.Probability >= MinProbability).ToList() ?? new List<PredictionModel>();

                //Dictionary<Guid, IEnumerable<PredictionModel>> objectsGroupedByTag = currentDetectedObjects.GroupBy(d => d.TagId).ToDictionary(d => d.Key, d => d.Select(x => x));
                //foreach (var tag in objectsGroupedByTag)
                //{
                //    DetectedObjectCollection.Add(new DetectedObjectsViewModel()
                //    {
                //        ObjectId = tag.Key,
                //        ObjectName = tag.Value.FirstOrDefault().TagName,
                //        ObjectCount = tag.Value.Count(),
                //        ObjectColor = Colors.Lime
                //    });
                //}

                //ShowObjectDetectionBoxes(currentDetectedObjects);
            }
        }

        private async Task<ImagePrediction> AnalyzeImageAsync(ProjectViewModel project, StorageFile file)
        {
            ImagePrediction result = null;

            try
            {
                this.progressRing.IsActive = true;

                var iteractions = await trainingApi.GetIterationsAsync(project.Id);
                var latestTrainedIteraction = iteractions.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();
                if (latestTrainedIteraction == null || string.IsNullOrEmpty(latestTrainedIteraction?.PublishName))
                {
                    throw new Exception("This project doesn't have any trained models or published iteration yet. Please train and publish it, or wait until training completes if one is in progress.");
                }

                using (Stream stream = (await file.OpenReadAsync()).AsStream())
                {
                    result = await CustomVisionServiceHelper.PredictImageWithRetryAsync(predictionApi, project.Id, latestTrainedIteraction.PublishName, stream);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "Analyze image error").ShowAsync();
            }
            finally
            {
                this.progressRing.IsActive = false;
            }

            return result;
        }


        private void OnObjectDetectionVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentDetectedObjects != null && this.objectDetectionVisualizationCanvas.Children.Any())
            {
                this.ShowObjectDetectionBoxes(currentDetectedObjects);
            }
        }

        private void ShowObjectDetectionBoxes(IEnumerable<PredictionModel> detectedObjects)
        {
            this.objectDetectionVisualizationCanvas.Children.Clear();

            double canvasWidth = objectDetectionVisualizationCanvas.ActualWidth;
            double canvasHeight = objectDetectionVisualizationCanvas.ActualHeight;

            foreach (PredictionModel prediction in detectedObjects)
            {
                objectDetectionVisualizationCanvas.Children.Add(
                    new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Lime),
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(prediction.BoundingBox.Left * canvasWidth,
                                               prediction.BoundingBox.Top * canvasHeight, 0, 0),
                        Width = prediction.BoundingBox.Width * canvasWidth,
                        Height = prediction.BoundingBox.Height * canvasHeight,
                    });
            }
        }

        private async Task LoadTagColorAsync()
        {
            try
            {
                var tags = await trainingApi.GetTagsAsync(currentProject.Id);
                currentProject.Tags = tags;
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "Tag loading error").ShowAsync();
            }
        }

        private async Task SaveImageToFileAsync(StorageFile file)
        {
            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
            {
                await imageCropper.SaveAsync(fileStream, BitmapFileFormat.Jpeg);
            }
        }

        private void OnCropImageButtonClicked(object sender, RoutedEventArgs e)
        {
            EnableCropFeature = !EnableCropFeature;
        }


        private void OnAddEditButtonClicked(object sender, RoutedEventArgs e)
        {
            enableEditMode = !enableEditMode;
            this.analyzeButton.IsEnabled = !enableEditMode;
            this.objectDetectionVisualizationCanvas.Visibility = enableEditMode ? Visibility.Collapsed : Visibility.Visible;
            this.imageRegionsCanvas.Visibility = enableEditMode ? Visibility.Visible : Visibility.Collapsed;

            this.imageRegionsCanvas.Children.Clear();

            if (enableEditMode)
            {
                foreach (PredictionModel obj in currentDetectedObjects)
                {
                    AddRegionToUI(obj);
                }
            }
        }

        private void AddRegionToUI(PredictionModel obj)
        {
            var editor = new RegionEditorControl
            {
                Width = image.ActualWidth * obj.BoundingBox.Width,
                Height = image.ActualHeight * obj.BoundingBox.Height,
                Margin = new Thickness(obj.BoundingBox.Left * image.ActualWidth, obj.BoundingBox.Top * image.ActualHeight, 0, 0),
                DataContext = new RegionEditorViewModel
                {
                    Region = obj,
                    AvailableTags = currentProject.Tags,
                    Color = Colors.Lime
                }
            };

            editor.RegionChanged += OnRegionChanged;
            editor.RegionDeleted += OnRegionDeleted;

            this.imageRegionsCanvas.Children.Add(editor);
        }

        private void OnRegionChanged(object sender, Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Tag tag)
        {
            RegionEditorControl regionControl = (RegionEditorControl)sender;

            RegionEditorViewModel regionEditorViewModel = (RegionEditorViewModel)regionControl.DataContext;
        }

        private void OnRegionDeleted(object sender, EventArgs e)
        {
            RegionEditorControl regionControl = (RegionEditorControl)sender;
            this.imageRegionsCanvas.Children.Remove(regionControl);
        }

        private void OnPointerReleasedOverImage(object sender, PointerRoutedEventArgs e)
        {
            if (enableEditMode)
            {
                var clickPosition = e.GetCurrentPoint(this.imageRegionsCanvas);

                double normalizedPosX = clickPosition.Position.X / imageRegionsCanvas.ActualWidth;
                double normalizedPosY = clickPosition.Position.Y / imageRegionsCanvas.ActualHeight;
                double normalizedWidth = 50 / imageRegionsCanvas.ActualWidth;
                double normalizedHeight = 50 / imageRegionsCanvas.ActualHeight;

                //ImageViewModel imageViewModel = (ImageViewModel)this.DataContext;
                //imageViewModel.AddedImageRegions.Add(newRegion);

                PredictionModel obj = new PredictionModel(
                    boundingBox: new BoundingBox(normalizedPosX, normalizedPosY,
                                        normalizedWidth + normalizedPosX > 1 ? 1 - normalizedPosX : normalizedWidth,
                                        normalizedHeight + normalizedPosY > 1 ? 1 - normalizedPosY : normalizedHeight));

                this.AddRegionToUI(obj);
            }
        }
    }
}
