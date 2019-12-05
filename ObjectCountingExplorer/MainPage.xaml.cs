using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using ObjectCountingExplorer.Helpers;
using ObjectCountingExplorer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ObjectCountingExplorer
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public const double MinMediumProbability = 0.3;
        public const double MinHighProbability = 0.6;

        private static readonly string TrainingApiKey = "";           // CUSTOM VISION TRANING API KEY
        private static readonly string TrainingApiKeyEndpoint = "";   // CUSTOM VISION TRANING API ENDPOINT
        private static readonly string PredictionApiKey = "";         // CUSTOM VISION PREDICTION API KEY
        private static readonly string PredictionApiKeyEndpoint = ""; // CUSTOM VISION PREDICTION API ENDPOINT

        private SummaryViewState currentSummaryGroupItem;
        private ProjectViewModel currentProject;
        private List<ProductItemViewModel> currentDetectedObjects;

        private CustomVisionTrainingClient trainingApi;
        private CustomVisionPredictionClient predictionApi;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SummaryGroupItem> SummaryGroupCollection { get; set; } = new ObservableCollection<SummaryGroupItem>
        {
            new SummaryGroupItem { Name = "result category", State = SummaryViewState.GroupedByCategory },
            new SummaryGroupItem { Name = "object tag", State = SummaryViewState.GroupedByTag }
        };

        public static readonly List<ProductFilter> ProductFilterByCategory = new List<ProductFilter>()
        {
            new ProductFilter("Low confidence", FilterType.LowConfidence),
            new ProductFilter("Medium confidence", FilterType.MediumConfidence),
            new ProductFilter("High confidence", FilterType.HighConfidence),
            new ProductFilter("Unknown product", FilterType.UnknownProduct),
            new ProductFilter("Shelf gap", FilterType.ShelfGap)
        };

        public ObservableCollection<ProjectViewModel> Projects { get; set; } = new ObservableCollection<ProjectViewModel>()
        {
            new ProjectViewModel(new Guid("af826f5b-97c1-40a0-b8bb-bf44e08cec2b"), "Product Reco Solution Template")
        };

        public ObservableCollection<ProductItemViewModel> LowConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> MediumConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> HighConfidenceCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> GeneralProductCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> ShelfGapCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> SelectedProductItemCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductTag> ProjectTagCollection { get; set; } = new ObservableCollection<ProductTag>();

        public ObservableCollection<ProductTag> RecentlyUsedTagCollection { get; set; } = new ObservableCollection<ProductTag>();

        public ObservableCollection<Tuple<string, List<ProductItemViewModel>>> GroupedProductCollection { get; set; } = new ObservableCollection<Tuple<string, List<ProductItemViewModel>>>();

        public ObservableCollection<ProductItemViewModel> AddedProductItems { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> EditedProductItems { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductItemViewModel> DeletedProductItems { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ResultDataGridViewModel> ResultDataGridCollection { get; set; } = new ObservableCollection<ResultDataGridViewModel>();

        public ObservableCollection<ProductFilter> ProductFilterCollection { get; set; } = new ObservableCollection<ProductFilter>();

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

        private SummaryViewState summaryViewState = SummaryViewState.GroupedByCategory;
        public SummaryViewState SummaryViewState
        {
            get { return summaryViewState; }
            set
            {
                summaryViewState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SummaryViewState"));
                SummaryViewStateChanged();
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
            }
            else
            {
                this.mainPage.IsEnabled = false;
                await new MessageDialog("Please enter Custom Vision API Keys in the code behind of this demo.", "Missing API Keys").ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private void SummaryViewStateChanged()
        {
            bool isAnySelectedProduct = SelectedProductItemCollection.Any();
            this.editRegionButton.IsEnabled = isAnySelectedProduct;
            this.clearSelectionButton.IsEnabled = isAnySelectedProduct;
            this.removeRegionButton.IsEnabled = isAnySelectedProduct;

            switch (SummaryViewState)
            {
                case SummaryViewState.GroupedByCategory:
                    this.chartControl.Visibility = Visibility.Visible;
                    this.resultsGrid.Visibility = Visibility.Collapsed;

                    this.productGroupedByCategoryGrid.Visibility = Visibility.Visible;
                    this.productGroupedByNameGrid.Visibility = Visibility.Collapsed;

                    ProductFilterCollection.Clear();
                    ProductFilterCollection.AddRange(ProductFilterByCategory);
                    break;

                case SummaryViewState.GroupedByTag:
                    this.chartControl.Visibility = Visibility.Collapsed;
                    this.resultsGrid.Visibility = Visibility.Visible;

                    this.productGroupedByCategoryGrid.Visibility = Visibility.Collapsed;
                    this.productGroupedByNameGrid.Visibility = Visibility.Visible;

                    ProductFilterCollection.Clear();
                    ProductFilterCollection.AddRange(GroupedProductCollection.Select(p => new ProductFilter(p.Item1, FilterType.ProductName)));
                    break;

                case SummaryViewState.CategorySelected:
                case SummaryViewState.TagSelected:
                default:
                    break;
            }
        }

        private void AppViewStateChanged()
        {
            this.statusRowDefinition.Height = new GridLength(0, GridUnitType.Auto);
            this.imageRowDefinition.Height = new GridLength(0.6, GridUnitType.Star);
            this.footerRowDefinition.Height = new GridLength(0, GridUnitType.Auto);

            this.imageColumnDefinition.Width = new GridLength(0.7, GridUnitType.Star);
            this.resultColumnDefinition.Width = new GridLength(0, GridUnitType.Auto);

            this.leftOffsetColumnDefinition.Width = new GridLength(0, GridUnitType.Auto);
            this.rightOffsetColumnDefinition.Width = new GridLength(0, GridUnitType.Auto);

            this.image.EnableImageControls = AppViewState != AppViewState.ImageAnalysisReview && AppViewState != AppViewState.ImageAnalysisPublish;

            switch (AppViewState)
            {
                case AppViewState.ImageSelected:
                    this.resultRowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                    break;

                case AppViewState.ImageAnalyzed:
                    this.resultRowDefinition.Height = new GridLength(0.4, GridUnitType.Star);
                    break;

                case AppViewState.ImageAddOrUpdateProduct:
                case AppViewState.ImageAnalysisReview:
                    this.resultRowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                    this.resultColumnDefinition.Width = new GridLength(0.3, GridUnitType.Star);
                    this.reviewGrid.Background = new SolidColorBrush(Color.FromArgb(255, 43, 43, 43));
                    break;

                case AppViewState.ImageAnalysisPublish:
                    this.statusRowDefinition.Height = new GridLength(0.4, GridUnitType.Star);
                    this.imageRowDefinition.Height = new GridLength(0.5, GridUnitType.Star);
                    this.resultRowDefinition.Height = new GridLength(0, GridUnitType.Auto);
                    this.footerRowDefinition.Height = new GridLength(0.15, GridUnitType.Star);

                    this.imageColumnDefinition.Width = new GridLength(0.3, GridUnitType.Star);
                    this.resultColumnDefinition.Width = new GridLength(0.2, GridUnitType.Star);
                    this.leftOffsetColumnDefinition.Width = new GridLength(0.2, GridUnitType.Star);
                    this.rightOffsetColumnDefinition.Width = new GridLength(0.2, GridUnitType.Star);

                    this.reviewGrid.Background = new SolidColorBrush(Colors.Transparent);
                    break;

                default:
                    break;
            }
        }

        private async void OnCloseImageViewButtonClicked(object sender, RoutedEventArgs e)
        {
            bool close = false;
            bool anyResults = AddedProductItems.Any() || EditedProductItems.Any() || DeletedProductItems.Any();
            if (anyResults)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Are you sure?",
                    Content = "It looks like you have been editing something. If you leave before publishing, your changes will be lost.",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Secondary
                };

                ContentDialogResult result = await dialog.ShowAsync();
                close = (result == ContentDialogResult.Primary);
            }
            
            if (close || !anyResults)
            {
                this.image.ClearSource();
                AppViewState = AppViewState.ImageSelection;
            }
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

                if (this.image.ImageFile is StorageFile currentImageFile)
                {
                    ImagePrediction result = await CustomVisionServiceHelper.AnalyzeImageAsync(trainingApi, predictionApi, currentProject.Id, currentImageFile);
                    currentDetectedObjects = (result?.Predictions?.ToList() ?? new List<PredictionModel>())
                        .Select(x => new ProductItemViewModel()
                        {
                            DisplayName = x.TagName, // can modify a product name for display name
                            Model = x
                        }).ToList();

                    ProjectTagCollection.Clear();
                    ProjectTagCollection.AddRange((await CustomVisionServiceHelper.GetTagsAsync(trainingApi, currentProject.Id))
                        .OrderBy(t => t.Name).Select(t => new ProductTag(t)));

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

                UpdateFilters();
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

        private void UpdateFilters()
        {
            GroupedProductCollection.Clear();
            var groupedProductCollection = currentDetectedObjects.GroupBy(p => p.DisplayName)
                .OrderBy(p => p.Key)
                .Select(p => new Tuple<string, List<ProductItemViewModel>>(p.Key, p.ToList()));
            GroupedProductCollection.AddRange(groupedProductCollection);

            ProductFilterCollection.Clear();
            ProductFilterCollection.AddRange(summaryViewState == SummaryViewState.GroupedByCategory
                ? ProductFilterByCategory.Select(p => new ProductFilter(p.Name, p.FilterType))
                : GroupedProductCollection.Select(p => new ProductFilter(p.Item1, FilterType.ProductName)));
        }

        private void UpdateResult(IEnumerable<ProductItemViewModel> productItemCollection)
        {
            SelectedProductItemCollection.Clear();

            GeneralProductCollection.Clear();
            GeneralProductCollection.AddRange(productItemCollection.Where(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase)));

            ShelfGapCollection.Clear();
            ShelfGapCollection.AddRange(productItemCollection.Where(p => p.DisplayName.Equals(Util.EmptyGapName, StringComparison.OrdinalIgnoreCase)));

            var unknownItems = GeneralProductCollection.Concat(ShelfGapCollection).Select(p => p.Id);
            LowConfidenceCollection.Clear();
            LowConfidenceCollection.AddRange(productItemCollection.Where(p => p.Model.Probability < MinMediumProbability && !unknownItems.Contains(p.Id)));

            MediumConfidenceCollection.Clear();
            MediumConfidenceCollection.AddRange(productItemCollection.Where(p => p.Model.Probability >= MinMediumProbability && p.Model.Probability < MinHighProbability && !unknownItems.Contains(p.Id)));

            HighConfidenceCollection.Clear();
            HighConfidenceCollection.AddRange(productItemCollection.Where(p => p.Model.Probability >= MinHighProbability && !unknownItems.Contains(p.Id)));

            this.chartControl.UpdateChart(productItemCollection);

            LoadResultsDataGrid(productItemCollection);

            this.image.SelectedRegions.Clear();
            this.image.ShowObjectDetectionBoxes(productItemCollection);
            this.image.ToggleEditState();

            AppViewState = AppViewState.ImageAnalyzed;

            switch (SummaryViewState)
            {
                case SummaryViewState.CategorySelected:
                    SummaryViewState = SummaryViewState.GroupedByCategory;
                    break;
                case SummaryViewState.TagSelected:
                    SummaryViewState = SummaryViewState.GroupedByTag;
                    break;
            }
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
                    var productToRemove = SelectedProductItemCollection.FirstOrDefault(p => p.Id == product.Id);
                    if (productToRemove != null)
                    {
                        SelectedProductItemCollection.Remove(productToRemove);
                    }
                }
            }

            SummaryViewState = SelectedProductItemCollection.Any()
                ? currentSummaryGroupItem == SummaryViewState.GroupedByCategory ? SummaryViewState.CategorySelected : SummaryViewState.TagSelected
                : currentSummaryGroupItem;
        }

        private void OnClearSelectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.image.ClearSelectedRegions();
            SelectedProductItemCollection.Clear();
            SummaryViewState = currentSummaryGroupItem;
        }

        private void OnCancelProductSelectionButtonClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ProductItemViewModel productItem)
            {
                this.image.UnSelectRegion(productItem);
                SelectedProductItemCollection.Remove(productItem);

                if (!SelectedProductItemCollection.Any())
                {
                    SummaryViewState = currentSummaryGroupItem;
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
                        var newProductItem = AddedProductItems.FirstOrDefault(p => p.Id == product.Id);

                        if (!DeletedProductItems.Any(p => p.Id == product.Id) && newProductItem == null)
                        {
                            DeletedProductItems.Add(product);
                        }

                        if (newProductItem != null)
                        {
                            AddedProductItems.Remove(newProductItem);
                        }

                        var productToRemove = currentDetectedObjects.FirstOrDefault(p => p.Id == product.Id);
                        if (productToRemove != null)
                        {
                            currentDetectedObjects.Remove(productToRemove);
                        }
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
            string buttonTag = ((Button)sender).Tag as string ?? string.Empty;

            if (buttonTag == "Add")
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

            RecentlyUsedTagCollection.Clear();
            foreach (var tagId in SettingsHelper.Instance.RecentlyUsedProducts)
            {
                var tag = ProjectTagCollection.FirstOrDefault(p => p.Tag.Id.ToString() == tagId);
                if (tag != null)
                {
                    RecentlyUsedTagCollection.Add(tag);
                }
            }

            AppViewState = AppViewState.ImageAddOrUpdateProduct;
        }

        private async void OnProductEditorControlProductUpdated(object sender, Tuple<UpdateMode, ProductTag> args)
        {
            if (args != null)
            {
                switch (args.Item1)
                {
                    case UpdateMode.UpdateExistingProduct:
                        foreach (var item in SelectedProductItemCollection)
                        {
                            item.DisplayName = args.Item2.Tag.Name;
                            item.Model = new PredictionModel(probability: 1.0, item.Model.TagId, item.Model.TagName, item.Model.BoundingBox);
                        }

                        this.image.ToggleEditState(SelectedProductItemCollection);
                        break;

                    case UpdateMode.UpdateNewProduct:
                        var productTag = args.Item2;
                        var newProduct = new ProductItemViewModel()
                        {
                            DisplayName = productTag.Tag.Name,
                            Model = new PredictionModel(tagId: productTag.Tag.Id, tagName: productTag.Tag.Name)
                        };
                        this.image.ShowNewObjects(newProduct);
                        break;

                    case UpdateMode.SaveExistingProduct:
                        if (this.image.ImageFile is StorageFile currentImageFile && SelectedProductItemCollection.Any())
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

                                        if (!EditedProductItems.Any(p => p.Id == product.Id))
                                        {
                                            EditedProductItems.Add(product);
                                        }
                                    }
                                }
                            }
                        }

                        UpdateResult(currentDetectedObjects);
                        break;

                    case UpdateMode.SaveNewProduct:
                        if (this.image.ImageFile is StorageFile imageFile && this.image.AddedNewObjects.Any())
                        {
                            using (var stream = (await imageFile.OpenStreamForReadAsync()).AsRandomAccessStream())
                            {
                                double imageWidth = this.image.PixelWidth;
                                double imageHeight = this.image.PixelHeight;

                                foreach (var item in this.image.AddedNewObjects)
                                {
                                    bool isNewProduct = !currentDetectedObjects.Any(p => p.Id == item.Id);
                                    if (isNewProduct)
                                    {
                                        BoundingBox boundingBox = item.Model.BoundingBox;
                                        item.DisplayName = args.Item2.Tag.Name;

                                        var rect = new Rect(imageWidth * boundingBox.Left, imageHeight * boundingBox.Top, imageWidth * boundingBox.Width, imageHeight * boundingBox.Height);
                                        item.Image = await Util.GetCroppedBitmapAsync(stream, rect);

                                        AddedProductItems.Add(item);
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

        private async void OnOpenResultsViewButtonClick(object sender, RoutedEventArgs e)
        {
            bool anyResults = AddedProductItems.Any() || EditedProductItems.Any() || DeletedProductItems.Any();
            if (anyResults)
            {
                LoadResultsDataGrid(currentDetectedObjects);

                this.image.ShowObjectDetectionBoxes(currentDetectedObjects, RegionState.Disabled);
                AppViewState = AppViewState.ImageAnalysisReview;
            }
            else
            {
                await new MessageDialog("It looks like you didn't make any corrections.", "Publishing results").ShowAsync();
            }
        }

        private void LoadResultsDataGrid(IEnumerable<ProductItemViewModel> productlist)
        {
            ResultDataGridCollection.Clear();

            var productListGroupedByName = productlist.GroupBy(p => p.DisplayName).OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.ToList());
            foreach (var item in productListGroupedByName)
            {
                string productName = item.Key;
                int totalCount = item.Value.Count;
                int lowConfCount = LowConfidenceCollection.Count(p => p.DisplayName == productName);
                int medConfCount = MediumConfidenceCollection.Count(p => p.DisplayName == productName);
                int highConfCount = HighConfidenceCollection.Count(p => p.DisplayName == productName);
                ResultDataGridCollection.Add(new ResultDataGridViewModel()
                {
                    Name = productName,
                    LowConfidenceCount = lowConfCount,
                    MediumConfidenceCount = medConfCount,
                    HighConfidenceCount = highConfCount,
                    TotalCount = totalCount
                });
            }
            var totalRow = new ResultDataGridViewModel()
            {
                Name = "Total",
                LowConfidenceCount = ResultDataGridCollection.Sum(r => r.LowConfidenceCount),
                MediumConfidenceCount = ResultDataGridCollection.Sum(r => r.MediumConfidenceCount),
                HighConfidenceCount = ResultDataGridCollection.Sum(r => r.HighConfidenceCount),
                TotalCount = ResultDataGridCollection.Sum(r => r.TotalCount)
            };
            ResultDataGridCollection.Add(totalRow);
        }

        private void OnPublishResultsCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.image.ShowObjectDetectionBoxes(currentDetectedObjects);
            AppViewState = AppViewState.ImageAnalyzed;
        }

        private async void OnPublishResultsButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.progressRing.IsActive = true;
                this.publishStatus.Text = "Publishing results";
                this.publishDetails.Text = "The image, results and corrections are being uploaded to your Custom Vision portal.";

                this.currentProjectTextBlock.Text = this.currentProject.Name;

                string[] unknownNames = new string[] { "none", "unknown" };
                int unknownProductCount = currentDetectedObjects.Count(x => unknownNames.Contains(x.DisplayName.ToLower()));
                int taggedProductCount = currentDetectedObjects.Count - unknownProductCount;
                string[] finalResults = new string[] 
                {
                    $"{taggedProductCount} tagged",
                    $"{unknownProductCount} unknown"
                };
                this.finalResultsTextBlock.Text = $"{currentDetectedObjects.Count} objects ({string.Join(", ", finalResults)})";

                string[] corrections = new string[] 
                {
                    EditedProductItems.Any() ? $"{EditedProductItems.Count} item(s) edited" : string.Empty,
                    AddedProductItems.Any() ? $"{AddedProductItems.Count} item(s) added" : string.Empty,
                    DeletedProductItems.Any() ? $"{DeletedProductItems.Count} item(s) deleted" : string.Empty
                };
                this.correctionsTextBlock.Text = string.Join(", ", corrections.Where(x => x.Length > 0));
                AppViewState = AppViewState.ImageAnalysisPublish;

                await CustomVisionServiceHelper.AddImageRegionsAsync(trainingApi, currentProject.Id, this.image.ImageFile, currentDetectedObjects);

                this.publishStatus.Text = "Results published";
                this.publishDetails.Text = "The image, results and corrections are now available in your Custom Vision portal.";
            }
            catch (Exception ex)
            {
                this.publishStatus.Text = "Publishing failed";
                this.publishDetails.Text = string.Empty;
                await Util.GenericApiCallExceptionHandler(ex, "Publishing results error");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private void OnTryAnotherImageButtonClick(object sender, RoutedEventArgs e)
        {
            this.image.ClearSource();
            AppViewState = AppViewState.ImageSelection;
        }

        private void OnSummaryGroupComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.summaryGroupComboBox.SelectedValue is SummaryViewState selectedSummaryGroupState)
            {
                this.currentSummaryGroupItem = selectedSummaryGroupState;
                SummaryViewState = selectedSummaryGroupState;
            }
        }

        private void ProductFilterChecked(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }

        private void ProductFilterUnchecked(object sender, RoutedEventArgs e)
        {
            this.ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filterData = currentDetectedObjects;
            var activeFilters = ProductFilterCollection.Where(f => f.IsChecked.GetValueOrDefault()).ToList();
            
            if (activeFilters.Any())
            {
                var tempData = new List<ProductItemViewModel>();
                foreach (var filter in activeFilters)
                {
                    switch (filter.FilterType)
                    {
                        case FilterType.HighConfidence:
                            tempData.AddRange(currentDetectedObjects.Where(p => p.Model.Probability >= MinHighProbability));
                            break;

                        case FilterType.MediumConfidence:
                            tempData.AddRange(currentDetectedObjects.Where(p => p.Model.Probability >= MinMediumProbability && p.Model.Probability < MinHighProbability));
                            break;

                        case FilterType.LowConfidence:
                            tempData.AddRange(currentDetectedObjects.Where(p => p.Model.Probability < MinMediumProbability));
                            break;

                        case FilterType.UnknownProduct:
                            tempData.AddRange(currentDetectedObjects.Where(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase)));
                            break;

                        case FilterType.ShelfGap:
                            tempData.AddRange(currentDetectedObjects.Where(p => p.DisplayName.Equals(Util.EmptyGapName, StringComparison.OrdinalIgnoreCase)));
                            break;

                        case FilterType.ProductName:
                            tempData.AddRange(currentDetectedObjects.Where(p => p.DisplayName == filter.Name));
                            break;
                    }
                }

                filterData = tempData;
            }

            UpdateResult(filterData);
        }

        private void ResetImageData()
        {
            this.image.ClearSource();
            this.currentDetectedObjects = null;

            LowConfidenceCollection.Clear();
            MediumConfidenceCollection.Clear();
            HighConfidenceCollection.Clear();
            SelectedProductItemCollection.Clear();
            ProjectTagCollection.Clear();
            RecentlyUsedTagCollection.Clear();
            GroupedProductCollection.Clear();
            AddedProductItems.Clear();
            EditedProductItems.Clear();
            DeletedProductItems.Clear();
            ProductFilterCollection.Clear();
            ResultDataGridCollection.Clear();
        }
    }
}
