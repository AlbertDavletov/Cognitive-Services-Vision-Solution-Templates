using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using ShelfAuditingAutomation.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace ShelfAuditingAutomation.Controls
{
    public sealed partial class ImageWithRegionEditorsControl : UserControl, INotifyPropertyChanged
    {
        private bool enableRemoveMode = false;
        private bool addNewRegionMode = false;
        private readonly static int DefaultBoxSize = 50;
        private readonly static Thickness ImagePadding = new Thickness(0, 50, 0, 50);

        private List<ProductItemViewModel> currentEditableObjects;
        private Tuple<RegionState, List<ProductItemViewModel>> currentDetectedObjects;

        public static readonly DependencyProperty ActiveImageControlsProperty =
            DependencyProperty.Register("ActiveImageControls",
                typeof(bool),
                typeof(ImageWithRegionEditorsControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty EnableImageControlsProperty =
            DependencyProperty.Register("EnableImageControls",
                typeof(Visibility),
                typeof(ImageWithRegionEditorsControl),
                new PropertyMetadata(Visibility.Visible));

        public bool ActiveImageControls
        {
            get { return (bool)GetValue(ActiveImageControlsProperty); }
            set { SetValue(ActiveImageControlsProperty, value); }
        }

        public Visibility EnableImageControls
        {
            get { return (Visibility)GetValue(EnableImageControlsProperty); }
            set { SetValue(EnableImageControlsProperty, value); }
        }

        public event EventHandler RegionSelected;
        public event EventHandler<Tuple<ActionType, ProductItemViewModel>> RegionActionSelected;

        public int PixelWidth { get; private set; }

        public int PixelHeight { get; private set; }

        public StorageFile ImageFile { get; private set; }

        public List<ProductItemViewModel> AddedNewObjects { get; set; } = new List<ProductItemViewModel>();

        public List<ProductItemViewModel> SelectedRegions { get; set; } = new List<ProductItemViewModel>();

        public event PropertyChangedEventHandler PropertyChanged;

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

        public ImageWithRegionEditorsControl()
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.scrollViewerMain.RegisterPropertyChangedCallback(ScrollViewer.ZoomFactorProperty, (s, e) =>
            {
                UpdateRegions();
            });
        }

        private async void ImageViewChanged()
        {
            if (ImageFile != null)
            {
                ActiveImageControls = !EnableCropFeature;

                if (EnableCropFeature)
                {
                    await this.imageCropper.LoadImageFromFile(ImageFile);
                }
                else
                {
                    await SetSourceFromFileAsync(ImageFile);
                }
            }
        }

        public async Task SetSourceFromFileAsync(StorageFile imagefile)
        {
            ClearSource();

            ImageFile = imagefile;
            this.imageNameTextBlock.Text = imagefile.Name;

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync((await imagefile.OpenStreamForReadAsync()).AsRandomAccessStream());

            this.image.Source = bitmapImage;
            PixelWidth = bitmapImage?.PixelWidth ?? 0;
            PixelHeight = bitmapImage?.PixelHeight ?? 0;
        }

        private void OnObjectDetectionVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentDetectedObjects != null && this.objectDetectionVisualizationCanvas.Children.Any())
            {
                this.ShowObjectDetectionBoxes(currentDetectedObjects.Item2, currentDetectedObjects.Item1, sizeChanged: true);
            }
        }

        private void OnEditObjectVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentEditableObjects != null && this.editObjectVisualizationCanvas.Children.Any())
            {
                this.ShowEditableObjectDetectionBoxes(currentEditableObjects, this.enableRemoveMode);
            }
        }

        public void ShowObjectDetectionBoxes(IEnumerable<ProductItemViewModel> detectedObjects, RegionState regionState = RegionState.Active, bool sizeChanged = false)
        {
            this.cropImageButton.Visibility = Visibility.Collapsed;
            this.clearSelectionButton.Visibility = Visibility.Visible;
            this.imageControlsPanel.HorizontalAlignment = HorizontalAlignment.Right;

            currentDetectedObjects = new Tuple<RegionState, List<ProductItemViewModel>>(regionState, detectedObjects.ToList());

            double canvasWidth = objectDetectionVisualizationCanvas.ActualWidth;
            double canvasHeight = objectDetectionVisualizationCanvas.ActualHeight;

            var existingObjects = this.objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList();
            var objectsToRemove = existingObjects.Where(p => !detectedObjects.Select(d => d.Id).Contains(p.ProductItemViewModel.Id)).ToList();
            foreach (var objToRemove in objectsToRemove)
            {
                this.objectDetectionVisualizationCanvas.Children.Remove(objToRemove);
            }

            foreach (var detectedObj in detectedObjects)
            {
                var model = detectedObj.Model;
                bool isLowConfidenceItem = Util.IsLowConfidenceRegion(model);

                RegionState state = regionState;
                if (state != RegionState.Disabled)
                {
                    bool isSelected = SelectedRegions.Any(x => x.Id == detectedObj.Id);
                    state = GetStateByModel(model, defaultState: state);
                    if (isSelected && (state == RegionState.Active || state == RegionState.LowConfidence))
                    {
                        state = RegionState.Selected;
                    }
                }

                ObjectRegionControl region = existingObjects.FirstOrDefault(d => d.ProductItemViewModel.Id == detectedObj.Id);
                if (region == null)
                {
                    region = new ObjectRegionControl();
                    objectDetectionVisualizationCanvas.Children.Add(region);
                }

                region.Title = detectedObj.DisplayName;
                region.Image = Util.GetCanonicalImage(detectedObj.CanonicalImagesBaseUrl, detectedObj.Model?.TagName);
                region.State = sizeChanged ? region.State : state;
                region.ProductItemViewModel = detectedObj;
                region.Color = Util.GetObjectRegionColor(model);
                region.ZoomValue = this.scrollViewerMain.ZoomFactor;
                region.EnableSelectedIcon = isLowConfidenceItem;
                region.Margin = new Thickness(model.BoundingBox.Left * canvasWidth, model.BoundingBox.Top * canvasHeight, 0, 0);
                region.Width = model.BoundingBox.Width * canvasWidth;
                region.Height = model.BoundingBox.Height * canvasHeight;

                region.RegionActionSelected -= OnRegionActionSelected;
                region.RegionSelected -= OnRegionSelected;

                if (regionState != RegionState.Disabled)
                {
                    region.RegionActionSelected += OnRegionActionSelected;
                    region.RegionSelected += OnRegionSelected;
                }
            }

            if (regionState != RegionState.Disabled)
            {
                this.scrollViewerMain.ChangeView(null, null, 1f);
            }
        }

        public void ShowEditableObjectDetectionBoxes(IEnumerable<ProductItemViewModel> detectedObjects, bool removeOption = false)
        {
            this.enableRemoveMode = removeOption;
            currentEditableObjects = detectedObjects?.ToList();
            this.clearSelectionButton.IsEnabled = false;

            this.imageGrid.Padding = ImagePadding;
            this.editObjectVisualizationCanvas.Children.Clear();
            this.editObjectVisualizationCanvas.Visibility = Visibility.Visible;
            double canvasWidth = editObjectVisualizationCanvas.ActualWidth;
            double canvasHeight = editObjectVisualizationCanvas.ActualHeight;

            foreach (ProductItemViewModel obj in detectedObjects)
            {
                var model = obj.Model;
                var editor = new RegionEditorControl
                {
                    Width = model.BoundingBox.Width * canvasWidth,
                    Height = model.BoundingBox.Height * canvasHeight,
                    Margin = new Thickness(model.BoundingBox.Left * canvasWidth, model.BoundingBox.Top * canvasHeight, 0, 0),
                    DataContext = new RegionEditorViewModel
                    {
                        ProductId = obj.Id,
                        Title = obj.DisplayName,
                        Model = model,
                        EnableRemove = removeOption,
                        ZoomValue = this.scrollViewerMain.ZoomFactor
                    }
                };

                editor.RegionChanged += OnRegionChanged;
                editor.RegionDeleted += OnRegionDeleted;

                this.editObjectVisualizationCanvas.Children.Add(editor);
            }
        }

        public void ToggleEditState(bool enable)
        {
            this.addNewRegionMode = enable;
            this.imageGrid.Padding = enable ? ImagePadding : new Thickness(0);
            this.clearSelectionButton.IsEnabled = enable ? false : SelectedRegions.Any();
            this.editObjectVisualizationCanvas.Children.Clear();
            this.editObjectVisualizationCanvas.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateObjectBoxes(ProductTag tag, bool isNewObject = false)
        {
            Guid newId = tag.Tag.Id;
            string newName = tag.Tag.Name;

            var data = isNewObject ? AddedNewObjects : SelectedRegions;
            foreach (var item in data)
            {
                item.DisplayName = newName;
                item.Model = new PredictionModel(1.0, newId, newName, item.Model.BoundingBox);
            }
            this.ShowEditableObjectDetectionBoxes(data, removeOption: isNewObject);
        }

        private void OnPointerReleasedOverImage(object sender, PointerRoutedEventArgs e)
        {
            bool isAnyRegionsOnCanvas = this.editObjectVisualizationCanvas.Children.Any();
            if (addNewRegionMode && !isAnyRegionsOnCanvas)
            {
                AddedNewObjects.Clear();

                var clickPosition = e.GetCurrentPoint(this.editObjectVisualizationCanvas);

                double normalizedPosX = clickPosition.Position.X / editObjectVisualizationCanvas.ActualWidth;
                double normalizedPosY = clickPosition.Position.Y / editObjectVisualizationCanvas.ActualHeight;
                double normalizedWidth = DefaultBoxSize / editObjectVisualizationCanvas.ActualWidth;
                double normalizedHeight = DefaultBoxSize / editObjectVisualizationCanvas.ActualHeight;

                PredictionModel obj = new PredictionModel(probability: 1.0,
                    boundingBox: new BoundingBox(normalizedPosX, normalizedPosY,
                                        normalizedWidth + normalizedPosX > 1 ? 1 - normalizedPosX : normalizedWidth,
                                        normalizedHeight + normalizedPosY > 1 ? 1 - normalizedPosY : normalizedHeight));
                var product = new ProductItemViewModel()
                {
                    DisplayName = "product",
                    Model = obj
                };

                this.AddRegionToUI(product);
            }
        }

        private void AddRegionToUI(ProductItemViewModel product)
        {
            double canvasWidth = editObjectVisualizationCanvas.ActualWidth;
            double canvasHeight = editObjectVisualizationCanvas.ActualHeight;

            var model = product.Model;
            var editor = new RegionEditorControl
            {
                Width = model.BoundingBox.Width * canvasWidth,
                Height = model.BoundingBox.Height * canvasHeight,
                Margin = new Thickness(model.BoundingBox.Left * canvasWidth, model.BoundingBox.Top * canvasHeight, 0, 0),
                DataContext = new RegionEditorViewModel
                {
                    ProductId = product.Id,
                    Title = product.DisplayName,
                    Model = model,
                    EnableRemove = true,
                    ZoomValue = this.scrollViewerMain.ZoomFactor
                }
            };

            editor.RegionChanged += OnRegionChanged;
            editor.RegionDeleted += OnRegionDeleted;

            this.editObjectVisualizationCanvas.Children.Add(editor);
            AddedNewObjects.Add(product);
            currentEditableObjects?.Add(product);
        }

        private void OnRegionChanged(object sender, Guid productId)
        {
            if (sender is RegionEditorControl regionControl)
            {
                RegionEditorViewModel regionEditorViewModel = (RegionEditorViewModel)regionControl.DataContext;

                PredictionModel model = regionEditorViewModel.Model;

                // Update size in case is changed
                model.BoundingBox.Left = Util.EnsureValidNormalizedValue(regionControl.Margin.Left / image.ActualWidth);
                model.BoundingBox.Top = Util.EnsureValidNormalizedValue(regionControl.Margin.Top / image.ActualHeight);
                model.BoundingBox.Width = Util.EnsureValidNormalizedValue(regionControl.ActualWidth / image.ActualWidth);
                model.BoundingBox.Height = Util.EnsureValidNormalizedValue(regionControl.ActualHeight / image.ActualHeight);

                if (model.BoundingBox.Width + model.BoundingBox.Left > 1)
                {
                    model.BoundingBox.Width = 1 - model.BoundingBox.Left;
                }

                if (model.BoundingBox.Height + model.BoundingBox.Top > 1)
                {
                    model.BoundingBox.Height = 1 - model.BoundingBox.Top;
                }
            }
        }

        private void OnRegionDeleted(object sender, Guid productId)
        {
            if (sender is RegionEditorControl regionControl)
            {
                var product = AddedNewObjects.FirstOrDefault(p => p.Id == productId);
                if (product != null)
                {
                    AddedNewObjects.Remove(product);
                    currentEditableObjects?.Remove(product);
                }

                bool isRemoved = this.editObjectVisualizationCanvas.Children.Remove(regionControl);
                if (isRemoved)
                {
                    regionControl.RegionChanged -= OnRegionChanged;
                    regionControl.RegionDeleted -= OnRegionDeleted;
                }
            }
        }

        private void UpdateRegions()
        {
            var regionList = this.editObjectVisualizationCanvas.Children.Cast<RegionEditorControl>().ToList();
            foreach (var region in regionList)
            {
                RegionEditorViewModel regionEditorViewModel = (RegionEditorViewModel)region.DataContext;
                if (regionEditorViewModel != null)
                {
                    regionEditorViewModel.ZoomValue = this.scrollViewerMain.ZoomFactor;
                }
            }

            var objectList = this.objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList();
            foreach (var obj in objectList)
            {
                obj.ZoomValue = this.scrollViewerMain.ZoomFactor;
            }
        }


        #region Region selection
        public void UpdateSelectedRegions(IEnumerable<ProductItemViewModel> products)
        {
            if (products.Any())
            {
                SelectedRegions.Clear();
                SelectedRegions.AddRange(products);

                foreach (ObjectRegionControl region in objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList())
                {
                    var model = region.ProductItemViewModel?.Model;
                    if (SelectedRegions.Any(x => x.Id == region.ProductItemViewModel.Id))
                    {
                        bool isLowConfidenceItem = Util.IsLowConfidenceRegion(region.ProductItemViewModel?.Model);
                        region.State = RegionState.Selected;
                        region.EnableSelectedIcon = isLowConfidenceItem;
                    }
                    else if (region.State == RegionState.Selected)
                    {
                        region.State = GetStateByModel(model);
                        region.EnableSelectedIcon = false;
                    }
                }
                this.clearSelectionButton.IsEnabled = SelectedRegions.Any();
                this.RegionSelected?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ClearSelectedRegions()
        {
            if (SelectedRegions.Any())
            {
                foreach (ObjectRegionControl region in objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList())
                {
                    var model = region.ProductItemViewModel?.Model;
                    region.State = GetStateByModel(model);
                    region.EnableSelectedIcon = false;
                }
                SelectedRegions.Clear();
            }
            this.clearSelectionButton.IsEnabled = false;
            this.RegionSelected?.Invoke(this, EventArgs.Empty);
        }

        private void OnClearSelectionButtonClick(object sender, RoutedEventArgs e)
        {
            ClearSelectedRegions();
        }

        private void OnRegionSelected(object sender, EventArgs args)
        {
            var selectedRegion = (ObjectRegionControl)sender;
            if (selectedRegion == null)
            {
                return;
            }

            var viewModel = selectedRegion.ProductItemViewModel;
            switch (selectedRegion.State)
            {
                case RegionState.Active:
                    selectedRegion.State = RegionState.Selected;
                    selectedRegion.EnableSelectedIcon = false;
                    SelectedRegions.Add(viewModel.DeepCopy());
                    break;

                case RegionState.LowConfidence:
                    bool isAnySelected = SelectedRegions.Any();
                    selectedRegion.State = !isAnySelected ? RegionState.SelectedWithNotification : RegionState.Selected;
                    selectedRegion.EnableSelectedIcon = isAnySelected;
                    SelectedRegions.Add(viewModel.DeepCopy());

                    if (!isAnySelected)
                    {
                        UpdateRegionState(disable: true);
                    }
                    break;

                case RegionState.Selected:
                    selectedRegion.State = RegionState.Active;

                    var region = SelectedRegions.FirstOrDefault(r => r.Id == viewModel.Id);
                    SelectedRegions.Remove(region);

                    UpdateRegionState();
                    break;
            }

            this.clearSelectionButton.IsEnabled = SelectedRegions.Any();
            this.RegionSelected?.Invoke(this, EventArgs.Empty);
        }

        private void OnRegionActionSelected(object sender, ActionType actionType)
        {
            var selectedRegion = (ObjectRegionControl)sender;
            if (selectedRegion == null)
            {
                return;
            }

            var viewModel = selectedRegion?.ProductItemViewModel;
            switch (actionType)
            {
                case ActionType.Apply:
                    viewModel.Model = new PredictionModel(1.0, viewModel.Model.TagId, viewModel.Model.TagName, viewModel.Model.BoundingBox);
                    selectedRegion.State = RegionState.Active;
                    SelectedRegions.Remove(SelectedRegions.FirstOrDefault(r => r.Id == viewModel.Id));

                    UpdateRegionState();
                    this.clearSelectionButton.IsEnabled = SelectedRegions.Any();
                    break;

                case ActionType.Cancel:
                    selectedRegion.State = RegionState.LowConfidence;
                    SelectedRegions.Remove(SelectedRegions.FirstOrDefault(r => r.Id == viewModel.Id));

                    UpdateRegionState();
                    this.clearSelectionButton.IsEnabled = SelectedRegions.Any();
                    break;

                case ActionType.Edit:
                default:
                    break;
            }

            this.RegionActionSelected?.Invoke(this, new Tuple<ActionType, ProductItemViewModel>(actionType, viewModel));
        }

        private void UpdateRegionState(bool disable = false)
        {
            foreach (ObjectRegionControl obj in objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList())
            {
                if (!SelectedRegions.Any(x => x.Id == obj.ProductItemViewModel.Id))
                {
                    obj.State = disable ? RegionState.Disabled : GetStateByModel(obj.ProductItemViewModel?.Model);
                }
            }
        }
        #endregion


        #region Crop and Zoom Image
        private void OnCropImageButtonClicked(object sender, RoutedEventArgs e)
        {
            EnableCropFeature = !EnableCropFeature;
        }

        public async Task CropImage(bool crop = false)
        {
            if (crop)
            {
                await SaveImageToFileAsync(ImageFile);
            }
            else
            {
                this.imageCropper.Reset();
            }
            EnableCropFeature = false;
        }

        private async Task SaveImageToFileAsync(StorageFile file)
        {
            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
            {
                await imageCropper.SaveAsync(fileStream, BitmapFileFormat.Jpeg);
            }
        }

        private void ZoomFlyoutOpened(object sender, object e)
        {
            this.zoomSlider.Value = this.scrollViewerMain.ZoomFactor;
        }

        private void ZoomSliderValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (this.scrollViewerMain != null)
            {
                this.scrollViewerMain.ChangeView(null, null, (float)this.zoomSlider.Value, true);
            }
        }
        #endregion

        private RegionState GetStateByModel(PredictionModel model, RegionState defaultState = RegionState.Active)
        {
            return Util.IsLowConfidenceRegion(model) ? RegionState.LowConfidence : defaultState;
        }

        public void ClearSource()
        {
            this.objectDetectionVisualizationCanvas.Children.Clear();

            currentEditableObjects = null;
            currentDetectedObjects = null;
            SelectedRegions.Clear();
            AddedNewObjects.Clear();

            ImageFile = null;
            this.image.Source = null;
            this.imageCropper.Source = null;

            EnableCropFeature = false;
            this.cropImageButton.Visibility = Visibility.Visible;
            this.clearSelectionButton.Visibility = Visibility.Collapsed;
            this.imageControlsPanel.HorizontalAlignment = HorizontalAlignment.Center;

            ToggleEditState(enable: false);
        }
    }
}
