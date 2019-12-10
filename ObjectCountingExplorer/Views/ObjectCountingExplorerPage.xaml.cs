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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ObjectCountingExplorer.Views
{
    public sealed partial class ObjectCountingExplorerPage : Page, INotifyPropertyChanged
    {
        private static readonly string TrainingApiKey = "";           // CUSTOM VISION TRANING API KEY
        private static readonly string TrainingApiKeyEndpoint = "";   // CUSTOM VISION TRANING API ENDPOINT
        private static readonly string PredictionApiKey = "";         // CUSTOM VISION PREDICTION API KEY
        private static readonly string PredictionApiKeyEndpoint = ""; // CUSTOM VISION PREDICTION API ENDPOINT

        private SummaryViewState currentSummaryViewState;
        private ProjectViewModel currentProject;
        private List<ProductItemViewModel> currentDetectedObjects;
        private List<ProductItemViewModel> addedProductItems = new List<ProductItemViewModel>();
        private List<ProductItemViewModel> editedProductItems = new List<ProductItemViewModel>();
        private List<ProductItemViewModel> deletedProductItems = new List<ProductItemViewModel>();

        private CustomVisionTrainingClient trainingApi;
        private CustomVisionPredictionClient predictionApi;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Tuple<string, SummaryViewState>> SummaryGroupCollection { get; set; } = new ObservableCollection<Tuple<string, SummaryViewState>>
        {
            new Tuple<string, SummaryViewState>("result category", SummaryViewState.GroupedByCategory),
            new Tuple<string, SummaryViewState>("object tag", SummaryViewState.GroupedByTag)
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

        public ObservableCollection<ProductTag> ProjectTagCollection { get; set; } = new ObservableCollection<ProductTag>();
        public ObservableCollection<ProductItemViewModel> SelectedProductItemCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductTag> RecentlyUsedTagCollection { get; set; } = new ObservableCollection<ProductTag>();

        public ObservableCollection<ProductFilter> ProductFilterCollection { get; set; } = new ObservableCollection<ProductFilter>(ProductFilterByCategory);

        public ObservableCollection<ResultDataGridViewModel> ResultDataGridCollection { get; set; } = new ObservableCollection<ResultDataGridViewModel>();

        public ObservableCollection<Tuple<string, List<ProductItemViewModel>>> GroupedProductCollection { get; set; } = new ObservableCollection<Tuple<string, List<ProductItemViewModel>>>();

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

        public ObjectCountingExplorerPage()
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
            }
        }

        private void SummaryViewStateChanged()
        {
            bool isAnySelectedProduct = SelectedProductItemCollection.Any();
            this.editRegionButton.IsEnabled = isAnySelectedProduct;
            this.clearSelectionButton.IsEnabled = isAnySelectedProduct;
            this.removeRegionButton.IsEnabled = isAnySelectedProduct;
        }

        private async void OnCloseImageViewButtonClicked(object sender, RoutedEventArgs e)
        {
            bool close = false;
            bool anyResults = addedProductItems.Any() || editedProductItems.Any() || deletedProductItems.Any();
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
            try
            {
                this.progressRing.IsActive = true;
                this.analyzeButton.IsEnabled = false;
                this.imageFileName.Text = this.image.ImageFile.Name;

                // get detected objects
                ImagePrediction result = await CustomVisionServiceHelper.AnalyzeImageAsync(trainingApi, predictionApi, currentProject.Id, this.image.ImageFile);
                currentDetectedObjects = (result?.Predictions?.ToList() ?? new List<PredictionModel>())
                    .Select(p => new ProductItemViewModel()
                    {
                        DisplayName = p.TagName, // can modify a product name for display name
                        Model = p
                    }).ToList();

                // get all tags from the project
                ProjectTagCollection.Clear();
                ProjectTagCollection.AddRange((await CustomVisionServiceHelper.GetTagsAsync(trainingApi, currentProject.Id))
                                    .OrderBy(t => t.Name).Select(t => new ProductTag(t)));

                // get cropped image for each object
                using (var stream = (await this.image.ImageFile.OpenStreamForReadAsync()).AsRandomAccessStream())
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

                UpdateImageDetectedBoxes(currentDetectedObjects);
                this.chartControl.UpdateChart(currentDetectedObjects);
                SummaryViewState = SummaryViewState.GroupedByCategory;
                UpdateGroupedProductCollection(SummaryViewState, currentDetectedObjects);

                AppViewState = AppViewState.ImageAnalyzed;
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

            SummaryViewState = SelectedProductItemCollection.Any() ? SummaryViewState.SelectedItems : currentSummaryViewState;
        }

        private void OnClearSelectionButtonClick(object sender, RoutedEventArgs e)
        {
            this.image.ClearSelectedRegions();
            SelectedProductItemCollection.Clear();
            foreach(var filter in ProductFilterCollection)
            {
                filter.IsChecked = false;
            }
            SummaryViewState = currentSummaryViewState;
        }

        private void OnCancelProductSelectionButtonClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is ProductItemViewModel productItem)
            {
                this.image.UnSelectRegion(productItem);
                SelectedProductItemCollection.Remove(productItem);

                if (!SelectedProductItemCollection.Any())
                {
                    SummaryViewState = currentSummaryViewState;
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
                        var newProductItem = addedProductItems.FirstOrDefault(p => p.Id == product.Id);

                        if (!deletedProductItems.Any(p => p.Id == product.Id) && newProductItem == null)
                        {
                            deletedProductItems.Add(product);
                        }

                        if (newProductItem != null)
                        {
                            addedProductItems.Remove(newProductItem);
                        }

                        var productToRemove = currentDetectedObjects.FirstOrDefault(p => p.Id == product.Id);
                        if (productToRemove != null)
                        {
                            currentDetectedObjects.Remove(productToRemove);
                        }
                    }

                    SelectedProductItemCollection.Clear();
                    UpdateImageDetectedBoxes(currentDetectedObjects);

                    SummaryViewState = SummaryViewState.GroupedByCategory;
                    AppViewState = AppViewState.ImageAnalyzed;
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
            this.image.ToggleEditState(enable: false);
            AppViewState = AppViewState.ImageAnalyzed;
        }

        private void OnAddOrEditProductButtonClick(object sender, RoutedEventArgs e)
        {
            string buttonTag = ((Button)sender).Tag as string ?? string.Empty;

            if (buttonTag == "Add")
            {
                this.productEditorControl.EditorState = EditorState.Add;
                this.image.ShowObjectDetectionBoxes(currentDetectedObjects, RegionState.Disabled);
                this.image.ToggleEditState(enable: true);
            }
            else
            {
                this.productEditorControl.EditorState = EditorState.Edit;
                var detectedProductsWithoutSelected = currentDetectedObjects.Where(p => !SelectedProductItemCollection.Select(s => s.Id).Contains(p.Id));
                this.image.ShowObjectDetectionBoxes(detectedProductsWithoutSelected, RegionState.Disabled);
                this.image.ShowEditableObjectDetectionBoxes(SelectedProductItemCollection);
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

        private async void OnProductEditorControlProductUpdated(object sender, Tuple<UpdateStatus, ProductTag> args)
        {
            UpdateStatus updateStatus = args.Item1;
            ProductTag tag = args.Item2;
            switch (updateStatus)
            {
                case UpdateStatus.UpdateExistingProduct:
                    foreach (var item in SelectedProductItemCollection)
                    {
                        item.DisplayName = tag.Tag.Name;
                        item.Model = new PredictionModel(probability: 1.0, item.Model.TagId, item.Model.TagName, item.Model.BoundingBox);
                    }
                    this.image.ShowEditableObjectDetectionBoxes(SelectedProductItemCollection);
                    break;

                case UpdateStatus.UpdateNewProduct:
                    this.image.UpdateNewObject(tag);
                    break;

                case UpdateStatus.SaveExistingProduct:
                    await UpdateProducts(SelectedProductItemCollection.Select(p => p.DeepCopy()));
                    SelectedProductItemCollection.Clear();

                    UpdateImageDetectedBoxes(currentDetectedObjects);
                    SummaryViewState = currentSummaryViewState;
                    AppViewState = AppViewState.ImageAnalyzed;
                    break;

                case UpdateStatus.SaveNewProduct:
                    await UpdateProducts(this.image.AddedNewObjects, newProducts: true);
                    this.image.AddedNewObjects.Clear();

                    UpdateImageDetectedBoxes(currentDetectedObjects);
                    SummaryViewState = currentSummaryViewState;
                    AppViewState = AppViewState.ImageAnalyzed;
                    break;
            }
        }

        private void OnSummaryGroupComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.summaryGroupComboBox.SelectedValue is SummaryViewState selectedSummaryGroupState && currentDetectedObjects != null)
            {
                UpdateImageDetectedBoxes(currentDetectedObjects);
                switch (selectedSummaryGroupState)
                {
                    case SummaryViewState.GroupedByCategory:
                        this.chartControl.UpdateChart(currentDetectedObjects);
                        UpdateGroupedProductCollection(SummaryViewState.GroupedByCategory, currentDetectedObjects);
                        break;

                    case SummaryViewState.GroupedByTag:
                        UpdateDataGrid(currentDetectedObjects);
                        UpdateGroupedProductCollection(SummaryViewState.GroupedByTag, currentDetectedObjects);
                        break;
                }
                this.chartControl.Visibility = selectedSummaryGroupState == SummaryViewState.GroupedByCategory ? Visibility.Visible : Visibility.Collapsed;
                this.resultsGrid.Visibility = selectedSummaryGroupState == SummaryViewState.GroupedByTag ? Visibility.Visible : Visibility.Collapsed;

                this.currentSummaryViewState = selectedSummaryGroupState;
                SummaryViewState = selectedSummaryGroupState;
            }
        }

        private void DataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.summaryDataGrid.SelectedItem is ResultDataGridViewModel selectedRow)
            {
                var filterData = selectedRow.IsAggregateColumn ? currentDetectedObjects : currentDetectedObjects.Where(p => p.DisplayName.Equals(selectedRow.Name, StringComparison.OrdinalIgnoreCase));
                UpdateGroupedProductCollection(SummaryViewState, filterData);
                UpdateImageDetectedBoxes(filterData);
            }
        }


        #region Publishing
        private async void OnOpenResultsViewButtonClick(object sender, RoutedEventArgs e)
        {
            bool anyResults = addedProductItems.Any() || editedProductItems.Any() || deletedProductItems.Any();
            if (anyResults)
            {
                UpdateDataGrid(currentDetectedObjects);

                this.image.ShowObjectDetectionBoxes(currentDetectedObjects, RegionState.Disabled);
                AppViewState = AppViewState.ImageAnalysisReview;
            }
            else
            {
                await new MessageDialog("It looks like you didn't make any corrections.", "Publishing results").ShowAsync();
            }
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

                int unknownProductsCount = currentDetectedObjects.Count(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase));
                int shelfGapsCount = currentDetectedObjects.Count(p => p.DisplayName.Equals(Util.EmptyGapName, StringComparison.OrdinalIgnoreCase));
                int taggedProductCount = currentDetectedObjects.Count - unknownProductsCount - shelfGapsCount;

                string[] finalResults = new string[]
                {
                    $"{taggedProductCount} tagged",
                    $"{unknownProductsCount} unknown",
                    $"{shelfGapsCount} shelf gaps",
                };
                this.finalResultsTextBlock.Text = $"{currentDetectedObjects.Count} objects ({string.Join(", ", finalResults)})";

                string[] corrections = new string[]
                {
                    editedProductItems.Any() ? $"{editedProductItems.Count} item(s) edited" : string.Empty,
                    addedProductItems.Any() ? $"{addedProductItems.Count} item(s) added" : string.Empty,
                    deletedProductItems.Any() ? $"{deletedProductItems.Count} item(s) deleted" : string.Empty
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
        #endregion


        #region Update Image, DataGrid, Collections, Products
        private void UpdateImageDetectedBoxes(IEnumerable<ProductItemViewModel> productItemCollection, IEnumerable<ProductItemViewModel> selectedItems = null)
        {
            if (selectedItems != null && selectedItems.Any())
            {
                this.image.UpdateSelectedRegions(selectedItems);
            }
            else
            {
                this.image.ClearSelectedRegions();
            }

            this.image.ShowObjectDetectionBoxes(productItemCollection);
            this.image.ToggleEditState(enable: false);
        }

        private void UpdateGroupedProductCollection(SummaryViewState viewState, IEnumerable<ProductItemViewModel> productItemCollection)
        {
            switch (viewState)
            {
                case SummaryViewState.GroupedByCategory:
                    var unknownProducts = productItemCollection.Where(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase)).ToList();
                    var shelfGaps = productItemCollection.Where(p => p.DisplayName.Equals(Util.EmptyGapName, StringComparison.OrdinalIgnoreCase)).ToList();
                    var unknownItems = unknownProducts.Concat(shelfGaps).Select(p => p.Id);
                    var lowConfidenceItems = productItemCollection.Where(p => p.Model.Probability < Util.MinMediumProbability && !unknownItems.Contains(p.Id)).ToList();
                    var mediumConfidenceItems = productItemCollection.Where(p => p.Model.Probability >= Util.MinMediumProbability && p.Model.Probability < Util.MinHighProbability && !unknownItems.Contains(p.Id)).ToList();
                    var highConfidenceItems = productItemCollection.Where(p => p.Model.Probability >= Util.MinHighProbability && !unknownItems.Contains(p.Id)).ToList();

                    GroupedProductCollection.Clear();
                    GroupedProductCollection.Add(new Tuple<string, List<ProductItemViewModel>>("Low Confidence", lowConfidenceItems));
                    GroupedProductCollection.Add(new Tuple<string, List<ProductItemViewModel>>("Medium Confidence", mediumConfidenceItems));
                    GroupedProductCollection.Add(new Tuple<string, List<ProductItemViewModel>>("High Confidence", highConfidenceItems));
                    GroupedProductCollection.Add(new Tuple<string, List<ProductItemViewModel>>("Unknown product", unknownProducts));
                    GroupedProductCollection.Add(new Tuple<string, List<ProductItemViewModel>>("Shelf gap", shelfGaps));
                    break;

                case SummaryViewState.GroupedByTag:
                    var groupedProductCollection = productItemCollection.GroupBy(p => p.DisplayName)
                        .OrderBy(p => p.Key)
                        .Select(p => new Tuple<string, List<ProductItemViewModel>>(p.Key, p.ToList()));
                    GroupedProductCollection.Clear();
                    GroupedProductCollection.AddRange(groupedProductCollection);
                    break;
            }
        }

        private void UpdateDataGrid(IEnumerable<ProductItemViewModel> productlist)
        {
            ResultDataGridCollection.Clear();

            var productListGroupedByName = productlist.GroupBy(p => p.DisplayName).OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.ToList());
            foreach (var item in productListGroupedByName)
            {
                string productName = item.Key;
                var products = item.Value;
                int totalCount = item.Value.Count;

                ResultDataGridCollection.Add(new ResultDataGridViewModel()
                {
                    Name = productName,
                    TotalCount = totalCount
                });
            }
            var totalRow = new ResultDataGridViewModel()
            {
                Name = "Total",
                TotalCount = ResultDataGridCollection.Sum(r => r.TotalCount),
                IsAggregateColumn = true
            };
            ResultDataGridCollection.Add(totalRow);
        }

        private async Task UpdateProducts(IEnumerable<ProductItemViewModel> products, bool newProducts = false)
        {
            if (!products.Any())
            {
                return;
            }

            using (var stream = (await this.image.ImageFile.OpenStreamForReadAsync()).AsRandomAccessStream())
            {
                double imageWidth = this.image.PixelWidth;
                double imageHeight = this.image.PixelHeight;

                foreach (var item in products)
                {
                    BoundingBox boundingBox = item.Model.BoundingBox;
                    var rect = new Rect(imageWidth * boundingBox.Left, imageHeight * boundingBox.Top, imageWidth * boundingBox.Width, imageHeight * boundingBox.Height);

                    if (newProducts)
                    {
                        bool isNewProduct = !currentDetectedObjects.Any(p => p.Id == item.Id);
                        if (isNewProduct)
                        {
                            item.DisplayName = item.DisplayName;
                            item.Image = await Util.GetCroppedBitmapAsync(stream, rect);

                            addedProductItems.Add(item);
                            currentDetectedObjects.Add(item);
                        }
                    }
                    else
                    {
                        var product = currentDetectedObjects.FirstOrDefault(p => p.Id == item.Id);
                        if (product != null)
                        {
                            product.DisplayName = item.DisplayName;
                            product.Model = item.Model;
                            product.Image = await Util.GetCroppedBitmapAsync(stream, rect);

                            if (!editedProductItems.Any(p => p.Id == product.Id))
                            {
                                editedProductItems.Add(product);
                            }
                        }
                    }
                }
            }
        }
        #endregion


        #region Checkbox Filters
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
            SelectedProductItemCollection.Clear();

            var activeFilters = ProductFilterCollection.Where(f => f.IsChecked).ToList();
            if (activeFilters.Any())
            {
                var shelfGaps = currentDetectedObjects.Where(p => p.DisplayName.Equals(Util.EmptyGapName, StringComparison.OrdinalIgnoreCase));
                var unknownProducts = currentDetectedObjects.Where(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase));
                var unknownItems = unknownProducts.Concat(unknownProducts).Select(p => p.Id).ToList();
                foreach (var filter in activeFilters)
                {
                    switch (filter.FilterType)
                    {
                        case FilterType.HighConfidence:
                            SelectedProductItemCollection.AddRange(currentDetectedObjects.Where(p => p.Model.Probability >= Util.MinHighProbability && !unknownItems.Contains(p.Id)));
                            break;

                        case FilterType.MediumConfidence:
                            SelectedProductItemCollection.AddRange(currentDetectedObjects.Where(p => p.Model.Probability >= Util.MinMediumProbability && p.Model.Probability < Util.MinHighProbability && !unknownItems.Contains(p.Id)));
                            break;

                        case FilterType.LowConfidence:
                            SelectedProductItemCollection.AddRange(currentDetectedObjects.Where(p => p.Model.Probability < Util.MinMediumProbability && !unknownItems.Contains(p.Id)));
                            break;

                        case FilterType.UnknownProduct:
                            SelectedProductItemCollection.AddRange(unknownProducts);
                            break;

                        case FilterType.ShelfGap:
                            SelectedProductItemCollection.AddRange(shelfGaps);
                            break;
                    }
                }
            }

            UpdateImageDetectedBoxes(currentDetectedObjects, SelectedProductItemCollection);
            SummaryViewState = SelectedProductItemCollection.Any() ? SummaryViewState.SelectedItems : currentSummaryViewState;
        }
        #endregion

        private void ResetImageData()
        {
            this.image.ClearSource();
            this.currentDetectedObjects = null;

            ProjectTagCollection.Clear();
            RecentlyUsedTagCollection.Clear();
            GroupedProductCollection.Clear();
            ResultDataGridCollection.Clear();
            SelectedProductItemCollection.Clear();

            addedProductItems.Clear();
            editedProductItems.Clear();
            deletedProductItems.Clear();
        }
    }
}
