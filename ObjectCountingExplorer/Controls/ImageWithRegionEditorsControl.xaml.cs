using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using ObjectCountingExplorer.Models;
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

namespace ObjectCountingExplorer.Controls
{
    public sealed partial class ImageWithRegionEditorsControl : UserControl, INotifyPropertyChanged
    {
        private bool useAllColors = true;
        private bool enableRemoveMode = false;
        private bool addNewRegionMode = false;
        private readonly static int DefaultBoxSize = 50;
        private readonly static Thickness ImagePadding = new Thickness(0, 50, 0, 50);

        private List<ProductItemViewModel> currentEditableObjects;
        private Tuple<RegionState, List<ProductItemViewModel>> currentDetectedObjects;
        private List<ProductItemViewModel> selectedRegions = new List<ProductItemViewModel>();

        public event EventHandler<Tuple<RegionState, ProductItemViewModel>> RegionSelected;

        public int PixelWidth { get; private set; }

        public int PixelHeight { get; private set; }

        public StorageFile ImageFile { get; private set; }

        public List<ProductItemViewModel> AddedNewObjects { get; set; } = new List<ProductItemViewModel>();

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

        private bool enableImageControls = true;
        public bool EnableImageControls
        {
            get { return enableImageControls; }
            set
            {
                enableImageControls = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EnableImageControls"));
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

        public async Task SetSourceFromFileAsync(StorageFile imagefile)
        {
            ClearSource();

            ImageFile = imagefile;

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync((await imagefile.OpenStreamForReadAsync()).AsRandomAccessStream());

            this.image.Source = bitmapImage;
            PixelWidth = bitmapImage?.PixelWidth ?? 0;
            PixelHeight = bitmapImage?.PixelHeight ?? 0;
        }

        public void ClearSource()
        {
            this.objectDetectionVisualizationCanvas.Children.Clear();

            currentEditableObjects = null;
            currentDetectedObjects = null;
            selectedRegions.Clear();

            ImageFile = null;
            this.image.Source = null;
            this.imageCropper.Source = null;

            EnableCropFeature = false;
            this.cropImageButton.Visibility = Visibility.Visible;

            ToggleEditState(enable: false);
        }

        private async void ImageViewChanged()
        {
            if (ImageFile != null)
            {
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

        private void OnObjectDetectionVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentDetectedObjects != null && this.objectDetectionVisualizationCanvas.Children.Any())
            {
                this.ShowObjectDetectionBoxes(currentDetectedObjects.Item2, currentDetectedObjects.Item1, this.useAllColors);
            }
        }

        private void OnEditObjectVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.currentEditableObjects != null && this.editObjectVisualizationCanvas.Children.Any())
            {
                this.ShowEditableObjectDetectionBoxes(currentEditableObjects, this.enableRemoveMode);
            }
        }

        public void ShowObjectDetectionBoxes(IEnumerable<ProductItemViewModel> detectedObjects, RegionState regionState = RegionState.Active, bool useAllColors = true)
        {
            this.cropImageButton.Visibility = Visibility.Collapsed;

            this.useAllColors = useAllColors;
            currentDetectedObjects = new Tuple<RegionState, List<ProductItemViewModel>>(regionState, detectedObjects.ToList());

            double canvasWidth = objectDetectionVisualizationCanvas.ActualWidth;
            double canvasHeight = objectDetectionVisualizationCanvas.ActualHeight;

            var existingObjects = this.objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList();
            var objectsToRemove = existingObjects.Where(p => !detectedObjects.Select(d => d.Id).Contains(p.ProductItemViewModel.Id)).ToList();
            foreach (var objToRemove in objectsToRemove)
            {
                // this.objectDetectionVisualizationCanvas.Children.Remove(objToRemove);
                objToRemove.State = RegionState.Collapsed;
            }

            foreach (var detectedObj in detectedObjects)
            {
                var model = detectedObj.Model;
                var state = regionState == RegionState.Disabled ? RegionState.Disabled
                                                                : selectedRegions.Any(x => x.Id == detectedObj.Id)
                                                                ? RegionState.Selected : RegionState.Active;

                ObjectRegionControl region = existingObjects.FirstOrDefault(d => d.ProductItemViewModel.Id == detectedObj.Id);
                if (region != null)
                {
                    region.Margin = new Thickness(model.BoundingBox.Left * canvasWidth, model.BoundingBox.Top * canvasHeight, 0, 0);
                    region.Width = model.BoundingBox.Width * canvasWidth;
                    region.Height = model.BoundingBox.Height * canvasHeight;
                    region.Title = detectedObj.DisplayName;
                    region.State = state;
                    region.ProductItemViewModel = detectedObj;
                    region.Color = Util.GetObjectRegionColor(model, useAllColors);
                    region.RegionSelected -= OnRegionSelected;
                }
                else
                {
                    region = new ObjectRegionControl
                    {
                        Margin = new Thickness(model.BoundingBox.Left * canvasWidth, model.BoundingBox.Top * canvasHeight, 0, 0),
                        Width = model.BoundingBox.Width * canvasWidth,
                        Height = model.BoundingBox.Height * canvasHeight,
                        Title = detectedObj.DisplayName,
                        State = state,
                        ProductItemViewModel = detectedObj,
                        Color = Util.GetObjectRegionColor(model, useAllColors)
                    };
                    objectDetectionVisualizationCanvas.Children.Add(region);
                }

                if (regionState != RegionState.Disabled)
                {
                    region.RegionSelected += OnRegionSelected;
                }
                else
                {
                    region.RegionSelected -= OnRegionSelected;
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
            this.editObjectVisualizationCanvas.Children.Clear();
            this.editObjectVisualizationCanvas.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateNewObject(ProductTag tag)
        {
            foreach (var item in AddedNewObjects)
            {
                item.DisplayName = tag.Tag.Name;
                item.Model = new PredictionModel(item.Model.Probability, tag.Tag.Id, tag.Tag.Name, item.Model.BoundingBox);
            }
            this.ShowEditableObjectDetectionBoxes(AddedNewObjects, true);
        }

        public void UpdateSelectedRegions(IEnumerable<ProductItemViewModel> products)
        {
            selectedRegions.Clear();
            selectedRegions.AddRange(products);
        }

        public void ClearSelectedRegions()
        {
            if (selectedRegions.Any())
            {
                foreach (ObjectRegionControl region in objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList())
                {
                    if (selectedRegions.Any(x => x.Id == region.ProductItemViewModel.Id))
                    {
                        region.State = RegionState.Active;
                    }
                }
                selectedRegions.Clear();
            }
        }

        public void UnSelectRegion(ProductItemViewModel item)
        {
            var objectRegion = objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().FirstOrDefault(x => x.ProductItemViewModel.Id == item.Id);
            if (objectRegion != null && objectRegion.State == RegionState.Selected)
            {
                objectRegion.State = RegionState.Active;
                selectedRegions.Remove(item);
            }
        }

        private void OnRegionSelected(object sender, Tuple<RegionState, ProductItemViewModel> item)
        {
            if (item.Item1 == RegionState.Selected)
            {
                selectedRegions.Add(item.Item2);
            }
            else
            {
                bool isRemoved = selectedRegions.Remove(item.Item2);
            }
            this.RegionSelected?.Invoke(this, item);
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
                    DisplayName = "None",
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
        }


        #region Crop and Zoom Image
        private void OnCropImageButtonClicked(object sender, RoutedEventArgs e)
        {
            EnableCropFeature = !EnableCropFeature;
        }

        private void OnCancelCropImageButtonClicked(object sender, RoutedEventArgs e)
        {
            this.imageCropper.Reset();
            EnableCropFeature = false;
        }

        private async void OnSaveImageButtonClicked(object sender, RoutedEventArgs e)
        {
            await SaveImageToFileAsync(ImageFile);
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
    }
}
