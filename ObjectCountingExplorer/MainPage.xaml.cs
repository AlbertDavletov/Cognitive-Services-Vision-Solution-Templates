using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Rest;
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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

        private ProjectViewModel currentProject;
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

        public ObservableCollection<ProjectViewModel> Projects { get; set; } = new ObservableCollection<ProjectViewModel>()
        {
            new ProjectViewModel(new Guid("eb3ba8ab-b716-44d4-950e-d9de7d5c92e7"), "GroceryItems")
        };

        public ObservableCollection<ProductItemViewModel> LowConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> MediumConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> HighConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> SelectedProductItemCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        private AppViewState appViewState = AppViewState.ImageSelection;
        public AppViewState AppViewState
        {
            get { return appViewState; }
            set
            {
                appViewState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AppViewState"));
                AppViewStateChanged();
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

                await LoadProjectsFromService();
            }
            else
            {
                this.mainPage.IsEnabled = true;
                await new MessageDialog("Please enter Custom Vision API Keys in the code behind of this demo.", "Missing API Keys").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async Task LoadProjectsFromService()
        {
            try
            {
                // Trigger loading of the tags associated with each project
                foreach (var project in this.Projects)
                {
                    project.TagSamples = new ObservableCollection<TagSampleViewModel>();
                    this.PopulateTagSamplesAsync(project.Id, this.trainingApi, project.TagSamples);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading projects");
            }
        }

        private async void PopulateTagSamplesAsync(Guid projectId, CustomVisionTrainingClient trainingEndPoint, ObservableCollection<TagSampleViewModel> collection)
        {
            var tags = (await trainingEndPoint.GetTagsAsync(projectId)).Take(5);
            foreach (var tag in tags.OrderBy(t => t.Name))
            {
                try
                {
                    if (tag.ImageCount > 0)
                    {
                        var imageModelSample = (await trainingEndPoint.GetTaggedImagesAsync(projectId, null, new List<Guid>() { tag.Id }, null, 1)).First();

                        var tagRegion = imageModelSample.Regions?.FirstOrDefault(r => r.TagId == tag.Id);
                        if (tagRegion == null || (tagRegion.Width == 0 && tagRegion.Height == 0))
                        {
                            collection.Add(new TagSampleViewModel { TagName = tag.Name, TagSampleImage = new BitmapImage(new Uri(imageModelSample.ThumbnailUri)) });
                        }
                        else
                        {
                            // Crop a region from the image that is associated with the tag, so we show something more 
                            // relevant than the whole image. 
                            ImageSource croppedImage = await Util.DownloadAndCropBitmapAsync(
                                imageModelSample.OriginalImageUri,
                                new Rect(
                                    tagRegion.Left * imageModelSample.Width, 
                                    tagRegion.Top * imageModelSample.Height,
                                    tagRegion.Width * imageModelSample.Width,
                                    tagRegion.Height * imageModelSample.Height));

                            collection.Add(new TagSampleViewModel { TagName = tag.Name, TagSampleImage = croppedImage });
                        }
                    }
                }
                catch (HttpOperationException exception) when (exception.Response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    continue;
                }
            }
        }

        private void RecognitionGroupChanged()
        {
            bool isAnySelectedProduct = SelectedProductItemCollection.Any();
            this.editRegionButton.IsEnabled = isAnySelectedProduct;
            this.clearSelectionButton.IsEnabled = isAnySelectedProduct;
            this.removeRegionButton.IsEnabled = isAnySelectedProduct;
        }

        private void AppViewStateChanged()
        {
            switch (AppViewState)
            {
                case AppViewState.ImageSelected:
                    this.resultRowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                    break;

                case AppViewState.ImageAnalyzed:
                    this.resultRowDefinition.Height = new GridLength(0.4, GridUnitType.Star);
                    break;

                default:
                    break;
            }
        }

        private void ResetImageData()
        {
            this.image.ClearSource();
            this.currentDetectedObjects = null;

            LowConfidenceCollection.Clear();
            MediumConfidenceCollection.Clear();
            HighConfidenceCollection.Clear();
            SelectedProductItemCollection.Clear();

            this.recognitionGroupListView.SelectedIndex = -1;
            this.chartControl.Visibility = Visibility.Collapsed;
        }

        private void OnCloseImageViewButtonClicked(object sender, RoutedEventArgs e)
        {
            this.image.ClearSource();
            AppViewState = AppViewState.ImageSelection;
        }

        private async void OnAnalyzeImageButtonClicked(object sender, RoutedEventArgs e)
        {
            if (currentProject == null)
            {
                return;
            }

            try
            {
                this.progressRing.IsActive = true;
                this.analyzeButton.IsEnabled = false;

                double imageW = this.image.PixelWidth;
                double imageH = this.image.PixelHeight;

                double tempW = imageW != 0 ? 60 / imageW : 60;
                double tempH = imageH != 0 ? 60 / imageH : 60;
                double marginX = imageW != 0 ? 5 / imageW : 5;
                double marginY = imageH != 0 ? 5 / imageH : 5;

                if (this.image.ImageFile is StorageFile currentImageFile)
                {
                    // ImagePrediction result = await AnalyzeImageAsync(currentProject, currentImageFile);
                    //currentDetectedObjects = result?.Predictions?.ToList() ?? new List<PredictionModel>();

                    currentDetectedObjects = CustomVisionServiceHelper.GetFakeTestData(tempW, tempH, marginX, marginY).Select(x => new ProductItemViewModel(x)).ToList();

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

                await Task.Delay(2000);

                UpdateResult(currentDetectedObjects);
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Analyze image error");
            }
            finally
            {
                this.progressRing.IsActive = false;
                this.analyzeButton.IsEnabled = true;
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

            this.image.ShowObjectDetectionBoxes(currentDetectedObjects);

            AppViewState = AppViewState.ImageAnalyzed;
        }

        private async Task<ImagePrediction> AnalyzeImageAsync(ProjectViewModel project, StorageFile file)
        {
            ImagePrediction result = null;

            try
            {
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
                await Util.GenericApiCallExceptionHandler(ex, "Custom Vision service error");
            }

            return result;
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

        private async void OnInputImageSelected(object sender, Tuple<ProjectViewModel, StorageFile> item)
        {
            ResetImageData();

            currentProject = item.Item1;
            await this.image.SetSourceFromFileAsync(item.Item2);

            AppViewState = AppViewState.ImageSelected;
        }
    }
}
