using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
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
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        private List<ProductItemViewModel> currentDetectedObjects;
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

        public ObservableCollection<ProductItemViewModel> SelectedProductItemCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<DetectedObjectsViewModel> DetectedObjectCollection { get; set; } = new ObservableCollection<DetectedObjectsViewModel>();

        private RecognitionGroup recognitionGroup;
        public RecognitionGroup RecognitionGroup
        {
            get { return recognitionGroup; }
            set
            {
                recognitionGroup = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RecognitionGroup"));
                RecognitionGroupChanged();
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

        private void RecognitionGroupChanged()
        {
            bool isAnySelectedProduct = SelectedProductItemCollection.Any();
            this.editRegionButton.IsEnabled = isAnySelectedProduct;
            this.clearSelectionButton.IsEnabled = isAnySelectedProduct;
            this.removeRegionButton.IsEnabled = isAnySelectedProduct;
        }

        private async void CameraControl_ImageCaptured(object sender, ImageAnalyzer img)
        {
            ResetImageData();

            StorageFile imageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Image.jpg", CreationCollisionOption.ReplaceExisting);

            if (img.ImageUrl != null)
            {
                await Util.DownloadAndSaveBitmapAsync(img.ImageUrl, imageFile);
            }
            else if (img.GetImageStreamCallback != null)
            {
                await Util.SaveBitmapToStorageFileAsync(await img.GetImageStreamCallback(), imageFile);
            }

            await this.image.SetSourceFromFileAsync(imageFile);

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
                    StorageFile imageFile = await selectedFile.CopyAsync(ApplicationData.Current.LocalFolder, "Image.jpg", NameCollisionOption.ReplaceExisting);

                    await this.image.SetSourceFromFileAsync(imageFile);

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
            this.image.ClearSource();
            this.currentDetectedObjects = null;

            DetectedObjectCollection.Clear();
            LowConfidenceCollection.Clear();
            MediumConfidenceCollection.Clear();
            HighConfidenceCollection.Clear();
            SelectedProductItemCollection.Clear();

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
            this.image.ClearSource();
            this.initialView.Visibility = Visibility.Visible;
            this.analyzeButton.Visibility = Visibility.Collapsed;
        }

        private async void OnAnalyzeImageButtonClicked(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                double imageW = this.image.PixelWidth;
                double imageH = this.image.PixelHeight;

                double tempW = imageW != 0 ? 60 / imageW : 60;
                double tempH = imageH != 0 ? 60 / imageH : 60;
                double marginX = imageW != 0 ? 5 / imageW : 5;
                double marginY = imageH != 0 ? 5 / imageH : 5;

                currentDetectedObjects = new List<ProductItemViewModel>()
                {
                    new ProductItemViewModel(new PredictionModel(probability: 0.99,  tagName: "General Mills", boundingBox: new BoundingBox(marginX, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.98,  tagName: "General Mills", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.97,  tagName: "General Mills", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.96,  tagName: "General Mills", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.95,  tagName: "General Mills", boundingBox: new BoundingBox(5 * marginX + 4 * tempW, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.94,  tagName: "General Mills", boundingBox: new BoundingBox(6 * marginX + 5 * tempW, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.6,   tagName: "General Mills", boundingBox: new BoundingBox(7 * marginX + 6 * tempW, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.59,  tagName: "General Mills", boundingBox: new BoundingBox(8 * marginX + 7 * tempW, marginY, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.25,  tagName: "General Mills", boundingBox: new BoundingBox(9 * marginX + 8 * tempW, marginY, tempW, tempH))),


                    new ProductItemViewModel(new PredictionModel(probability: 0.81,  tagName: "Great Value", boundingBox: new BoundingBox(marginX, 2 * marginY + tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.79,  tagName: "Great Value", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 2 * marginY + tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.78,  tagName: "Great Value", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 2 * marginY + tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.59,  tagName: "Great Value", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, 2 * marginY + tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.58,  tagName: "Great Value", boundingBox: new BoundingBox(5 * marginX + 4 * tempW, 2 * marginY + tempH, tempW, tempH))),


                    new ProductItemViewModel(new PredictionModel(probability: 0.99, tagName: "Quaker", boundingBox: new BoundingBox(marginX, 3 * marginY + 2 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.98, tagName: "Quaker", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 3 * marginY + 2 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.97, tagName: "Quaker", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 3 * marginY + 2 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.96, tagName: "Quaker", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, 3 * marginY + 2 * tempH, tempW, tempH))),


                    new ProductItemViewModel(new PredictionModel(probability: 0.8,  tagName: "Kellog", boundingBox: new BoundingBox(marginX, 4 * marginY + 3 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.81, tagName: "Kellog", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 4 * marginY + 3 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.82, tagName: "Kellog", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 4 * marginY + 3 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.7,  tagName: "Kellog", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, 4 * marginY + 3 * tempH, tempW, tempH))),


                    new ProductItemViewModel(new PredictionModel(probability: 0.8,  tagName: "None", boundingBox: new BoundingBox(marginX, 5 * marginY + 4 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.81, tagName: "None", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 5 * marginY + 4 * tempH, tempW, tempH))),
                    new ProductItemViewModel(new PredictionModel(probability: 0.82, tagName: "None", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 5 * marginY + 4 * tempH, tempW, tempH)))
                };

                if (this.image.ImageFile is StorageFile currentImageFile)
                {
                    using (var stream = (await currentImageFile.OpenStreamForReadAsync()).AsRandomAccessStream())
                    {
                        foreach (var product in currentDetectedObjects)
                        {
                            double imageWidth = this.image.PixelWidth;
                            double imageHeight = this.image.PixelHeight;

                            var rect = new Rect(imageWidth * product.Rect.Left, imageHeight * product.Rect.Top, imageWidth * product.Rect.Width, imageHeight * product.Rect.Height);
                            product.Image = await Util.GetCroppedBitmapAsync(stream, rect);
                        }
                    }
                }

                UpdateResult(currentDetectedObjects);
            }
        }

        private void UpdateResult(IEnumerable<ProductItemViewModel> productItemCollection)
        {

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
            //currentDetectedObjects = result?.Predictions?.Where(p => p.Probability >= 0.6).ToList() ?? new List<PredictionModel>();

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


            this.image.ShowObjectDetectionBoxes(currentDetectedObjects);
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






        private void OnAddEditButtonClicked(object sender, RoutedEventArgs e)
        {
            enableEditMode = !enableEditMode;
            this.analyzeButton.IsEnabled = !enableEditMode;

            if (enableEditMode)
            {
                //foreach (PredictionModel obj in currentDetectedObjects)
                //{
                //    // AddRegionToUI(obj);
                //}
            }
        }

        private void OnImageRegionSelected(object sender, Tuple<RegionState, ProductItemViewModel> item)
        {
            if (item.Item1 == RegionState.Selected)
            {
                SelectedProductItemCollection.Add(item.Item2);
            }
            else
            {
                SelectedProductItemCollection.Remove(item.Item2);
            }

            RecognitionGroup = SelectedProductItemCollection.Any() ? RecognitionGroup.SelectedItems : RecognitionGroup.Summary;
        }

        private void OnClearSelectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.image.ClearSelectedRegions();
            SelectedProductItemCollection.Clear();
            RecognitionGroup = RecognitionGroup.Summary;
        }

        private void OnCancelProductSelectionButtonClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ProductItemViewModel productItem)
            {
                this.image.UnSelectRegion(productItem);
                SelectedProductItemCollection.Remove(productItem);

                if (!SelectedProductItemCollection.Any())
                {
                    RecognitionGroup = RecognitionGroup.Summary;
                }
            }
        }

        private async void OnRemoveRegionButtonClick(object sender, RoutedEventArgs e)
        {
            if (SelectedProductItemCollection.Any())
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Delete selected product(s) permanently?",
                    Content = "This operation will delete product(s) permanently.\nAre you sure you want to continue?",
                    PrimaryButtonText = "Delete",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Secondary
                };

                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    foreach (ProductItemViewModel product in SelectedProductItemCollection)
                    {
                        currentDetectedObjects.Remove(product);
                    }

                    SelectedProductItemCollection.Clear();
                    UpdateResult(currentDetectedObjects);
                }
            }
        }
    }
}
