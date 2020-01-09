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

        private CustomVisionTrainingClient trainingApi;
        private CustomVisionPredictionClient predictionApi;

        private SpecsData currentSpec;
        private ProjectViewModel currentProject;
        private List<ProductItemViewModel> currentDetectedObjects;
        private List<ProductItemViewModel> addedProductItems = new List<ProductItemViewModel>();
        private List<ProductItemViewModel> editedProductItems = new List<ProductItemViewModel>();
        private List<ProductItemViewModel> deletedProductItems = new List<ProductItemViewModel>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SpecsData> SpecsDataCollection { get; set; } = new ObservableCollection<SpecsData>();

        public ObservableCollection<ProductTag> ProjectTagCollection { get; set; } = new ObservableCollection<ProductTag>();

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

                UpdateImageDetectedBoxes(currentDetectedObjects);
                UpdateChart(this.coverageChart, currentDetectedObjects);
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

        private void OnImageRegionSelected(object sender, EventArgs e)
        {
            this.editLabelButton.IsEnabled = this.image.SelectedRegions.Any();
            this.removeLabelButton.IsEnabled = this.image.SelectedRegions.Any();
        }

        private async void OnRemoveRegionButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.image.SelectedRegions.Any())
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
                    foreach (ProductItemViewModel product in this.image.SelectedRegions)
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

                    UpdateImageDetectedBoxes(currentDetectedObjects);
                    UpdateChart(this.coverageChart, currentDetectedObjects);
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

        private async void CropImageButtonClick(object sender, RoutedEventArgs e)
        {
            string buttonTag = ((Button)sender).Tag as string ?? string.Empty;
            await this.image.CropImage(crop: buttonTag == "Apply");
        }

        private void OnProductEditorControlClosed(object sender, EventArgs e)
        {
            foreach (var product in this.image.SelectedRegions)
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
                this.productEditorControl.CurrentTag = ProjectTagCollection.FirstOrDefault(p => p.Tag.Name.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase));
                this.image.ShowObjectDetectionBoxes(currentDetectedObjects, RegionState.Disabled);
                this.image.ToggleEditState(enable: true);
                this.image.AddedNewObjects.Clear();
            }
            else
            {
                this.productEditorControl.EditorState = EditorState.Edit;
                this.productEditorControl.CurrentTag = ProjectTagCollection.FirstOrDefault(p => p.Tag.Id == this.image.SelectedRegions.FirstOrDefault()?.Model?.TagId);
                var detectedProductsWithoutSelected = currentDetectedObjects.Where(p => !this.image.SelectedRegions.Select(s => s.Id).Contains(p.Id));
                this.image.ShowObjectDetectionBoxes(detectedProductsWithoutSelected, RegionState.Disabled);
                this.image.ShowEditableObjectDetectionBoxes(this.image.SelectedRegions);
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
                    this.image.UpdateObjectBoxes(tag, isNewObject: false);
                    break;

                case UpdateStatus.UpdateNewProduct:
                    this.image.UpdateObjectBoxes(tag, isNewObject: true);
                    break;

                case UpdateStatus.SaveExistingProduct:
                case UpdateStatus.SaveNewProduct:

                    bool isNewProducts = updateStatus == UpdateStatus.SaveNewProduct;
                    if (isNewProducts && this.image.AddedNewObjects.Any(x => x.Model?.TagId == Guid.Empty))
                    {
                        this.image.UpdateObjectBoxes(tag, isNewObject: true);
                    }
                    var data = isNewProducts ? this.image.AddedNewObjects : this.image.SelectedRegions;
                    await UpdateProducts(data, newProducts: isNewProducts);

                    UpdateImageDetectedBoxes(currentDetectedObjects);
                    UpdateChart(this.coverageChart, currentDetectedObjects);
                    UpdateDataGrid(currentDetectedObjects);
                    UpdateGroupedProductCollection(currentDetectedObjects);

                    AppViewState = AppViewState.ImageAnalysisReview;
                    break;
            }
        }

        #region Publishing

        private async void OnPublishButtonClicked(object sender, RoutedEventArgs e)
        {
            this.image.ShowObjectDetectionBoxes(currentDetectedObjects, RegionState.Disabled);
            await PublishResultsAsync();
        }

        private void PivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.pivot.SelectedIndex)
            {
                case 0:
                    this.dataGrid.Visibility = Visibility.Visible;
                    this.groupedProductListView.Visibility = Visibility.Collapsed;

                    this.dataGrid.SelectedItems?.Clear();
                    this.image.SelectedRegions.Clear();
                    break;

                case 1:
                    this.dataGrid.Visibility = Visibility.Collapsed;
                    this.groupedProductListView.Visibility = Visibility.Visible;
                    break;
            }

            UpdateImageDetectedBoxes(currentDetectedObjects);
        }

        private void ProductCollectionControlProductSelected(object sender, ProductItemViewModel e)
        {
            this.image.UpdateSelectedRegions(new List<ProductItemViewModel>() { e });
        }

        private void DataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRows = this.dataGrid.SelectedItems.Cast<ResultDataGridViewModel>();
            if (selectedRows.Any())
            {
                if (selectedRows.Any(r => r.IsAggregateColumn))
                {
                    UpdateImageDetectedBoxes(currentDetectedObjects);
                    this.dataGrid.SelectedItems.Clear();
                }
                else
                {
                    var filters = selectedRows.Where(r => !r.IsAggregateColumn).Select(r => r.Name.ToLower());
                    var filterData = selectedRows.All(r => r.IsAggregateColumn) ? currentDetectedObjects : currentDetectedObjects.Where(p => filters.Contains(p.DisplayName.ToLower()));
                    this.image.UpdateSelectedRegions(filterData);
                }
                
            }
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

                this.sourceImageTextBlock.Text = this.image.ImageFile.Name;
                this.currentProjectTextBlock.Text = this.currentProject.Name;
                UpdateChart(this.finalCoverageChart, currentDetectedObjects);

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
        #endregion


        #region Update Image, Chart, DataGrid, Collections, Products
        private void UpdateImageDetectedBoxes(IEnumerable<ProductItemViewModel> productItemCollection)
        {
            this.image.ClearSelectedRegions();
            this.image.ShowObjectDetectionBoxes(productItemCollection.Select(p => p.DeepCopy()));
            this.image.ToggleEditState(enable: false);
        }

        private void UpdateChart(CoverageChartControl chartControl, IEnumerable<ProductItemViewModel> productItemCollection)
        {
            if (productItemCollection != null && productItemCollection.Any())
            {
                double totalArea =           productItemCollection.Sum(p => p.Model.BoundingBox.Width * p.Model.BoundingBox.Height);
                double shelfGapArea =        productItemCollection.Where(p => p.DisplayName.Equals(Util.ShelfGapName, StringComparison.OrdinalIgnoreCase)).Sum(p => p.Model.BoundingBox.Width * p.Model.BoundingBox.Height);
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
                chartControl?.GenerateChart(data, $"{productItemCollection.Count()} items total");
            }
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

            var productListGroupedByName = productlist.GroupBy(p => p.DisplayName).OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.ToList());
            foreach (var item in productListGroupedByName)
            {
                Guid tagId = item.Value.First().Model.TagId;
                string productName = item.Key;
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

            addedProductItems.Clear();
            editedProductItems.Clear();
            deletedProductItems.Clear();
        }
    }
}
