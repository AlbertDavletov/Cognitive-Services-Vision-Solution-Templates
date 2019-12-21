using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using ShelfAuditingAutomation.Controls;
using ShelfAuditingAutomation.Helpers;
using ShelfAuditingAutomation.Models;
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
using Windows.UI.Xaml.Navigation;

namespace ShelfAuditingAutomation.Views
{
    public sealed partial class ShelfAuditingAutomationPage : Page, INotifyPropertyChanged
    {
        private static readonly string TrainingApiKey = "";           // CUSTOM VISION TRANING API KEY
        private static readonly string TrainingApiKeyEndpoint = "";   // CUSTOM VISION TRANING API ENDPOINT
        private static readonly string PredictionApiKey = "";         // CUSTOM VISION PREDICTION API KEY
        private static readonly string PredictionApiKeyEndpoint = ""; // CUSTOM VISION PREDICTION API ENDPOINT

        private SpecsData currentSpec;
        private ProjectViewModel currentProject;
        private List<ProductItemViewModel> currentDetectedObjects;
        private List<ProductItemViewModel> addedProductItems = new List<ProductItemViewModel>();
        private List<ProductItemViewModel> editedProductItems = new List<ProductItemViewModel>();
        private List<ProductItemViewModel> deletedProductItems = new List<ProductItemViewModel>();

        private CustomVisionTrainingClient trainingApi;
        private CustomVisionPredictionClient predictionApi;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SpecsData> SpecsDataCollection { get; set; } = new ObservableCollection<SpecsData>();

        public ObservableCollection<ProductTag> ProjectTagCollection { get; set; } = new ObservableCollection<ProductTag>();

        public ObservableCollection<ProductItemViewModel> SelectedProductItemCollection { get; set; } = new ObservableCollection<ProductItemViewModel>();

        public ObservableCollection<ProductTag> RecentlyUsedTagCollection { get; set; } = new ObservableCollection<ProductTag>();

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

        public ShelfAuditingAutomationPage()
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

                SpecsDataCollection.Clear();
                SpecsDataCollection.AddRange((await CustomSpecsDataLoader.GetData()) ?? CustomSpecsDataLoader.GetBuiltInData());

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
            this.headerRowDefinition.Height = AppViewState == AppViewState.ImageAnalysisPublishing ? new GridLength(0.4, GridUnitType.Star) : new GridLength(0, GridUnitType.Auto);
            this.footerRowDefinition.Height = AppViewState == AppViewState.ImageAnalysisPublishing ? new GridLength(0.15, GridUnitType.Star) : new GridLength(0, GridUnitType.Auto);

            this.imageColumnDefinition.Width =       AppViewState == AppViewState.ImageAnalysisPublishing ? new GridLength(0.3, GridUnitType.Star) : new GridLength(0.7, GridUnitType.Star);
            this.leftOffsetColumnDefinition.Width =  AppViewState == AppViewState.ImageAnalysisPublishing ? new GridLength(0.2, GridUnitType.Star) : new GridLength(0, GridUnitType.Auto);
            this.rightOffsetColumnDefinition.Width = AppViewState == AppViewState.ImageAnalysisPublishing ? new GridLength(0.2, GridUnitType.Star) : new GridLength(0, GridUnitType.Auto);

            this.resultColumnDefinition.Width = new GridLength(0, GridUnitType.Auto);

            switch (AppViewState)
            {
                case AppViewState.ImageAddOrUpdateProduct:
                case AppViewState.ImageAnalysisReview:
                    this.resultColumnDefinition.Width = new GridLength(0.3, GridUnitType.Star);
                    break;

                case AppViewState.ImageAnalysisPublishing:
                    this.resultColumnDefinition.Width = new GridLength(0.2, GridUnitType.Star);
                    break;
            }
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

                UpdateChart(currentDetectedObjects);
                UpdateImageDetectedBoxes(currentDetectedObjects);
                UpdateDataGrid(currentDetectedObjects);
                UpdateGroupedProductCollection(currentDetectedObjects);

                AppViewState = AppViewState.ImageAnalysisReview;
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Analyze image error");
            }
            finally
            {
                this.progressRing.IsActive = false;
            }
        }

        private void UpdateChart(IEnumerable<ProductItemViewModel> productItemCollection)
        {
            if (productItemCollection != null && productItemCollection.Any())
            {
                double totalArea = productItemCollection.Sum(p => p.Model.BoundingBox.Width * p.Model.BoundingBox.Height);
                double shelfGapArea = productItemCollection.Where(p => p.DisplayName.Equals(Util.ShelfGapName, StringComparison.OrdinalIgnoreCase)).Sum(p => p.Model.BoundingBox.Width * p.Model.BoundingBox.Height);
                double unknownProductsArea = productItemCollection.Where(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase)).Sum(p => p.Model.BoundingBox.Width * p.Model.BoundingBox.Height);
                double taggedProductsArea = totalArea - shelfGapArea - unknownProductsArea;

                var data = new List<ChartItem>()
                {
                    new ChartItem()
                    {
                        Name = "Tagged item",
                        Value = totalArea > 0 ? taggedProductsArea / totalArea : 0,
                        Background = new Windows.UI.Xaml.Media.SolidColorBrush(Util.TaggedItemColor)
                    },
                    new ChartItem()
                    {
                        Name = "Unknown item",
                        Value = totalArea > 0 ? unknownProductsArea / totalArea : 0,
                        Background = new Windows.UI.Xaml.Media.SolidColorBrush(Util.UnknownProductColor)
                    },
                    new ChartItem()
                    {
                        Name = "Shelf gap",
                        Value = totalArea > 0 ? shelfGapArea / totalArea : 0,
                        Background = new Windows.UI.Xaml.Media.SolidColorBrush(Util.ShelfGapColor),
                        Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Colors.Black)
                    }
                };
                this.coverageChart.GenerateChart(data, $"{productItemCollection.Count()} items total");
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
                    UpdateDataGrid(currentDetectedObjects);
                    UpdateGroupedProductCollection(currentDetectedObjects);

                    AppViewState = AppViewState.ImageAnalysisReview;
                }
            }
        }

        private async void OnInputImageSelected(object sender, Tuple<SpecsData, StorageFile> args)
        {
            ResetImageData();

            // get project and image from input view page
            currentSpec = args.Item1;
            currentProject = new ProjectViewModel(args.Item1.ModelId, args.Item1.ModelName);
            await this.image.SetSourceFromFileAsync(args.Item2);

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
            AppViewState = AppViewState.ImageAnalysisReview;
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
                        item.Model = new PredictionModel(probability: 1.0, tag.Tag.Id, tag.Tag.Name, item.Model.BoundingBox);
                    }
                    this.image.ShowEditableObjectDetectionBoxes(SelectedProductItemCollection);
                    break;

                case UpdateStatus.UpdateNewProduct:
                    this.image.UpdateNewObject(tag);
                    break;

                case UpdateStatus.SaveExistingProduct:
                case UpdateStatus.SaveNewProduct:

                    if (updateStatus == UpdateStatus.SaveExistingProduct)
                    {
                        await UpdateProducts(SelectedProductItemCollection.Select(p => p.DeepCopy()));
                        SelectedProductItemCollection.Clear();
                    }
                    else
                    {
                        await UpdateProducts(this.image.AddedNewObjects, newProducts: true);
                        this.image.AddedNewObjects.Clear();
                    }

                    UpdateImageDetectedBoxes(currentDetectedObjects);
                    UpdateDataGrid(currentDetectedObjects);
                    UpdateGroupedProductCollection(currentDetectedObjects);

                    AppViewState = AppViewState.ImageAnalysisReview;
                    break;
            }
        }

        #region Publishing

        private async void OnPublishButtonClicked(object sender, RoutedEventArgs e)
        {
            bool anyResults = addedProductItems.Any() || editedProductItems.Any() || deletedProductItems.Any();
            if (anyResults)
            {
                this.image.ShowObjectDetectionBoxes(currentDetectedObjects, RegionState.Disabled);
                await PublishResultsAsync();
            }
            else
            {
                await new MessageDialog("It looks like you didn't make any corrections.", "Publishing results").ShowAsync();
            }
        }

        private void PivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.pivot.SelectedIndex)
            {
                case 0:
                    this.dataGrid.Visibility = Visibility.Visible;
                    this.groupedProductListView.Visibility = Visibility.Collapsed;

                    SelectedProductItemCollection.Clear();
                    ApplyFilters();
                    break;

                case 1:
                    this.dataGrid.Visibility = Visibility.Collapsed;
                    this.groupedProductListView.Visibility = Visibility.Visible;

                    ApplyFilters(useFilters: false);
                    break;
            }
        }

        private void ProductCollectionControlProductSelected(object sender, ProductItemViewModel e)
        {
            if (e != null)
            {
                SelectedProductItemCollection.Clear();
                SelectedProductItemCollection.Add(e);
                this.image.UpdateSelectedRegions(SelectedProductItemCollection);
            }
        }

        private void FilterChecked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterUnchecked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters(bool useFilters = true)
        {
            var selectedRows = useFilters ? ResultDataGridCollection.Where(f => f.IsChecked) : ResultDataGridCollection;
            var filterData = currentDetectedObjects.Where(p => selectedRows.Select(r => r.Name.ToLower()).Contains(p.DisplayName.ToLower()));
            UpdateImageDetectedBoxes(filterData);
        }

        private void OnTryAnotherImageButtonClick(object sender, RoutedEventArgs e)
        {
            this.image.ClearSource();
            AppViewState = AppViewState.ImageSelection;
        }

        private async Task PublishResultsAsync()
        {
            try
            {
                this.progressRing.IsActive = true;
                this.publishStatus.Text = "Publishing results";
                this.publishDetails.Text = "The image, results and corrections are being uploaded to your Custom Vision portal.";

                this.currentProjectTextBlock.Text = this.currentProject.Name;

                int unknownProductsCount = currentDetectedObjects.Count(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase));
                int shelfGapsCount = currentDetectedObjects.Count(p => p.DisplayName.Equals(Util.ShelfGapName, StringComparison.OrdinalIgnoreCase));
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
                bool isAnyCorrections = corrections.Any(x => x.Length > 0);
                this.correctionsTextBlock.Text = isAnyCorrections ? string.Join(", ", corrections.Where(x => x.Length > 0)) : "N/A";
                AppViewState = AppViewState.ImageAnalysisPublishing;

                if (isAnyCorrections)
                {
                    await CustomVisionServiceHelper.AddImageRegionsAsync(trainingApi, currentProject.Id, this.image.ImageFile, currentDetectedObjects);
                }

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

        private void UpdateGroupedProductCollection(IEnumerable<ProductItemViewModel> productItemCollection)
        {
            var groupedProductCollection = productItemCollection.GroupBy(p => p.DisplayName)
                .OrderBy(p => p.Key)
                .Select(p => new Tuple<string, List<ProductItemViewModel>>(p.Key, p.ToList()));
            GroupedProductCollection.Clear();
            GroupedProductCollection.AddRange(groupedProductCollection);
        }

        private void UpdateDataGrid(IEnumerable<ProductItemViewModel> productlist)
        {
            ResultDataGridCollection.Clear();

            int total = productlist.Count();
            var productListGroupedByName = productlist.GroupBy(p => p.DisplayName).OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.ToList());
            foreach (var item in productListGroupedByName)
            {
                Guid tagId = item.Value.First().Model.TagId;
                string productName = item.Key;
                var products = item.Value;
                int totalCount = item.Value.Count;
                int expectedCount = currentSpec.Items.Any(s => s.TagId == tagId) ? currentSpec.Items.First(s => s.TagId == tagId).ExpectedCount : 0;

                ResultDataGridCollection.Add(new ResultDataGridViewModel()
                {
                    Name = productName,
                    TotalCount = totalCount,
                    ExpectedCount = expectedCount
                });
            }
            var totalRow = new ResultDataGridViewModel()
            {
                Name = "Total",
                TotalCount = ResultDataGridCollection.Sum(r => r.TotalCount),
                ExpectedCount = ResultDataGridCollection.Sum(r => r.ExpectedCount),
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
