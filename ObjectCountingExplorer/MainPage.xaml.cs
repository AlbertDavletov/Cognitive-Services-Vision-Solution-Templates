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

        public ObservableCollection<ProductItemViewModel> UniqueProductItemCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> AddedProductItems { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> DeletedProductItems { get; set; } = new ObservableCollection<ProductItemViewModel>();

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
                    CustomVisionServiceHelper.PopulateTagSamplesAsync(this.trainingApi, project.Id, project.TagSamples);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure loading projects");
            }
        }

        private void RecognitionGroupChanged()
        {
            bool isAnySelectedProduct = SelectedProductItemCollection.Any();
            this.editRegionButton.IsEnabled = isAnySelectedProduct;
            this.clearSelectionButton.IsEnabled = isAnySelectedProduct;
            this.removeRegionButton.IsEnabled = isAnySelectedProduct;

            switch (RecognitionGroup)
            {
                case RecognitionGroup.Summary:
                    this.image.ShowObjectDetectionBoxes(currentDetectedObjects);
                    break;

                case RecognitionGroup.HighConfidence:
                    this.image.ShowObjectDetectionBoxes(HighConfidenceCollection);
                    break;

                case RecognitionGroup.MediumConfidence:
                    this.image.ShowObjectDetectionBoxes(MediumConfidenceCollection);
                    break;

                case RecognitionGroup.LowConfidence:
                    this.image.ShowObjectDetectionBoxes(LowConfidenceCollection);
                    break;
                default:
                    break;
            }
        }

        private void AppViewStateChanged()
        {
            this.resultColumnDefinition.Width = new GridLength(0, GridUnitType.Auto);

            switch (AppViewState)
            {
                case AppViewState.ImageSelected:
                    this.resultRowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                    break;

                case AppViewState.ImageAnalyzed:
                    this.resultRowDefinition.Height = new GridLength(0.4, GridUnitType.Star);
                    break;

                case AppViewState.ImageAddOrUpdateProduct:
                    this.resultRowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                    this.resultColumnDefinition.Width = new GridLength(0.3, GridUnitType.Star);
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
            UniqueProductItemCollection.Clear();
            AddedProductItems.Clear();
            DeletedProductItems.Clear();

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
                    // ImagePrediction result = await CustomVisionServiceHelper.AnalyzeImageAsync(trainingApi, predictionApi, currentProject.Id, currentImageFile);
                    //currentDetectedObjects = result?.Predictions?.ToList() ?? new List<PredictionModel>();

                    currentDetectedObjects = CustomVisionServiceHelper.GetFakeTestData(tempW, tempH, marginX, marginY)
                        .Select(x => new ProductItemViewModel()
                        {
                            DisplayName = x.TagName, // can modify a product name for display name
                            Model = x
                        }).ToList();

                    using (var stream = (await currentImageFile.OpenStreamForReadAsync()).AsRandomAccessStream())
                    {
                        foreach (var product in currentDetectedObjects)
                        {
                            double imageWidth = this.image.PixelWidth;
                            double imageHeight = this.image.PixelHeight;
                            BoundingBox boundingBox = product.Model.BoundingBox;

                            var rect = new Rect(imageWidth * boundingBox.Left, imageHeight * boundingBox.Top, imageWidth * boundingBox.Width, imageHeight * boundingBox.Height);
                            product.Image = await Util.GetCroppedBitmapAsync(stream, rect);
                        }
                    }
                }

                await Task.Delay(500);

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
            SelectedProductItemCollection.Clear();

            UniqueProductItemCollection.Clear();
            UniqueProductItemCollection.AddRange(productItemCollection.GroupBy(p => p.DisplayName).Select(p => p.FirstOrDefault()).OrderBy(p => p.DisplayName));

            LowConfidenceCollection.Clear();
            LowConfidenceCollection.AddRange(productItemCollection.Where(x => x.Model.Probability <= 0.3));

            MediumConfidenceCollection.Clear();
            MediumConfidenceCollection.AddRange(productItemCollection.Where(x => x.Model.Probability > 0.3 && x.Model.Probability <= 0.6));

            HighConfidenceCollection.Clear();
            HighConfidenceCollection.AddRange(productItemCollection.Where(x => x.Model.Probability > 0.6));

            this.recognitionGroupListView.SelectedIndex = 0;
            this.chartControl.Visibility = Visibility.Visible;
            this.chartControl.UpdateChart(productItemCollection);

            this.image.SelectedRegions.Clear();
            this.image.ShowObjectDetectionBoxes(currentDetectedObjects);
            this.image.ToggleEditState();

            AppViewState = AppViewState.ImageAnalyzed;
        }

        private void OnImageRegionSelected(object sender, Tuple<RegionState, ProductItemViewModel> args)
        {
            if (args?.Item1 != null && args?.Item2 != null)
            {
                ProductItemViewModel product = args.Item2.DeepCopy();
                if (args.Item1 == RegionState.Selected)
                {
                    SelectedProductItemCollection.Add(product);
                }
                else
                {
                    SelectedProductItemCollection.Remove(product);
                }
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
                        if (!DeletedProductItems.Any(p => p.Id == product.Id))
                        {
                            DeletedProductItems.Add(product);
                        }

                        if (AddedProductItems.Any(p => p.Id == product.Id))
                        {
                            AddedProductItems.Remove(product);
                        }

                        currentDetectedObjects.Remove(product);
                    }

                    SelectedProductItemCollection.Clear();
                    UpdateResult(currentDetectedObjects);
                }
            }
        }

        private async void OnInputImageSelected(object sender, Tuple<ProjectViewModel, StorageFile> inputData)
        {
            ResetImageData();

            // get project and image from input view page
            currentProject = inputData.Item1;
            await this.image.SetSourceFromFileAsync(inputData.Item2);

            AppViewState = AppViewState.ImageSelected;
        }

        private void OnProductEditorControlClosed(object sender, EventArgs e)
        {
            foreach (var product in SelectedProductItemCollection)
            {
                var originalProduct = currentDetectedObjects.FirstOrDefault(p => p.Id == product.Id)?.DeepCopy();
                product.DisplayName = originalProduct.DisplayName;
                product.Model = originalProduct.Model;
            }

            this.image.ShowObjectDetectionBoxes(currentDetectedObjects);
            this.image.ToggleEditState();
            AppViewState = AppViewState.ImageAnalyzed;
        }

        private void OnAddOrEditProductButtonClick(object sender, RoutedEventArgs e)
        {
            string tag = ((Button)sender).Tag as string ?? string.Empty;

            if (tag == "Add")
            {
                this.productEditorControl.EditorState = EditorState.Add;
                this.image.ShowObjectDetectionBoxes(currentDetectedObjects, RegionState.Disabled);
                this.image.ToggleEditState(enableRemoveOption: true);
            }
            else
            {
                this.productEditorControl.EditorState = EditorState.Edit;
                var detectedProductsWithoutSelected = currentDetectedObjects.Where(p => !SelectedProductItemCollection.Select(s => s.Id).Contains(p.Id));
                this.image.ShowObjectDetectionBoxes(detectedProductsWithoutSelected, RegionState.Disabled);
                this.image.ToggleEditState(SelectedProductItemCollection);
            }

            AppViewState = AppViewState.ImageAddOrUpdateProduct;
        }

        private async void OnProductEditorControlProductUpdated(object sender, Tuple<UpdateMode, ProductItemViewModel> args)
        {
            if (args != null)
            {
                switch (args.Item1)
                {
                    case UpdateMode.UpdateExistingProduct:
                        foreach (var item in SelectedProductItemCollection)
                        {
                            item.DisplayName = args.Item2.DisplayName;
                        }

                        this.image.ToggleEditState(SelectedProductItemCollection);
                        break;

                    case UpdateMode.UpdateNewProduct:
                        this.image.ShowNewObjects(args.Item2.DisplayName);
                        break;

                    case UpdateMode.SaveExistingProduct:
                        if (this.image.ImageFile is StorageFile currentImageFile)
                        {
                            using (var stream = (await currentImageFile.OpenStreamForReadAsync()).AsRandomAccessStream())
                            {
                                double imageWidth = this.image.PixelWidth;
                                double imageHeight = this.image.PixelHeight;

                                foreach (var item in SelectedProductItemCollection.Select(p => p.DeepCopy()))
                                {
                                    var product = currentDetectedObjects.FirstOrDefault(p => p.Id == item.Id);
                                    if (product != null)
                                    {
                                        BoundingBox boundingBox = product.Model.BoundingBox;

                                        product.DisplayName = item.DisplayName;
                                        product.Model = item.Model;

                                        var rect = new Rect(imageWidth * boundingBox.Left, imageHeight * boundingBox.Top, imageWidth * boundingBox.Width, imageHeight * boundingBox.Height);
                                        product.Image = await Util.GetCroppedBitmapAsync(stream, rect);
                                    }
                                }
                            }
                        }

                        UpdateResult(currentDetectedObjects);
                        break;

                    case UpdateMode.SaveNewProduct:
                        if (this.image.ImageFile is StorageFile imageFile)
                        {
                            using (var stream = (await imageFile.OpenStreamForReadAsync()).AsRandomAccessStream())
                            {
                                double imageWidth = this.image.PixelWidth;
                                double imageHeight = this.image.PixelHeight;

                                foreach (var item in this.image.AddedNewObjects.Select(p => p))
                                {
                                    bool isNewProduct = !currentDetectedObjects.Any(p => p.Id == item.Id);
                                    if (isNewProduct)
                                    {
                                        BoundingBox boundingBox = item.Model.BoundingBox;
                                        item.DisplayName = args.Item2.DisplayName;

                                        var rect = new Rect(imageWidth * boundingBox.Left, imageHeight * boundingBox.Top, imageWidth * boundingBox.Width, imageHeight * boundingBox.Height);
                                        item.Image = await Util.GetCroppedBitmapAsync(stream, rect);

                                        currentDetectedObjects.Add(item);
                                    }
                                }

                                this.image.AddedNewObjects.Clear();
                            }
                        }

                        UpdateResult(currentDetectedObjects);
                        break;
                }
            }

            
        }
    }
}
