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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ObjectCountingExplorer.Controls
{
    public sealed partial class ImageWithRegionEditorsControl : UserControl, INotifyPropertyChanged
    {
        
        public List<ProductItemViewModel> AddedNewObjects { get; set; } = new List<ProductItemViewModel>();

        private bool enableRemoveMode = false;
        private Tuple<RegionState, List<ProductItemViewModel>> currentDetectedObjects;
        private Tuple<bool, List<ProductItemViewModel>> selectedObjects;

        public event EventHandler<Tuple<RegionState, ProductItemViewModel>> RegionSelected;


        public int PixelWidth { get; private set; }

        public int PixelHeight { get; private set; }

        public StorageFile ImageFile { get; private set; }

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
            SelectedRegions.Clear();
            this.objectDetectionVisualizationCanvas.Children.Clear();

            ImageFile = null;
            this.image.Source = null;
            this.imageCropper.Source = null;
            currentDetectedObjects = null;
            selectedObjects = null;
            EnableCropFeature = false;
            this.cropImageButton.IsEnabled = true;
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
                this.ShowObjectDetectionBoxes(currentDetectedObjects.Item2, currentDetectedObjects.Item1);
            }
        }

        private void OnEditObjectVisualizationCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.selectedObjects != null && this.editObjectVisualizationCanvas.Children.Any())
            {
                this.ToggleEditState(selectedObjects.Item2, selectedObjects.Item1);
            }
        }

        public void ShowObjectDetectionBoxes(IEnumerable<ProductItemViewModel> detectedObjects, RegionState regionState = RegionState.Active)
        {
            this.cropImageButton.IsEnabled = false;

            currentDetectedObjects = new Tuple<RegionState, List<ProductItemViewModel>>(regionState, detectedObjects.ToList());
            this.objectDetectionVisualizationCanvas.Children.Clear();

            double canvasWidth = objectDetectionVisualizationCanvas.ActualWidth;
            double canvasHeight = objectDetectionVisualizationCanvas.ActualHeight;

            foreach (ProductItemViewModel obj in detectedObjects)
            {
                var model = obj.Model;
                var state = regionState == RegionState.Disabled ? RegionState.Disabled 
                                                                : SelectedRegions.Any() && SelectedRegions.Any(x => x.Id == obj.Id) 
                                                                ? RegionState.Selected : RegionState.Active;

                var region = new ObjectRegionControl
                {
                    Margin = new Thickness(model.BoundingBox.Left * canvasWidth, model.BoundingBox.Top * canvasHeight, 0, 0),
                    Width = model.BoundingBox.Width * canvasWidth,
                    Height = model.BoundingBox.Height * canvasHeight,

                    Title = obj.DisplayName,
                    State = state,
                    ProductItemViewModel = obj,
                    Color = GetObjectRegionColor(model)
                };

                if (regionState != RegionState.Disabled)
                {
                    region.RegionSelected += OnRegionSelected;
                }
                objectDetectionVisualizationCanvas.Children.Add(region);
            }

            if (regionState != RegionState.Disabled)
            {
                this.scrollViewerMain.ChangeView(null, null, 1f);
            }
        }

        public void ToggleEditState(IEnumerable<ProductItemViewModel> detectedObjects = null, bool enableRemoveOption = false)
        {
            this.enableRemoveMode = enableRemoveOption;
            selectedObjects = detectedObjects != null ? new Tuple<bool, List<ProductItemViewModel>>(enableRemoveOption, detectedObjects.ToList()) : null;

            this.overlayCanvas.Children.Clear();
            this.editObjectVisualizationCanvas.Children.Clear();

            bool isVisibleCanvas = enableRemoveOption || (detectedObjects != null && detectedObjects.Any());
            this.overlayCanvas.Visibility = isVisibleCanvas ? Visibility.Visible : Visibility.Collapsed;
            this.editObjectVisualizationCanvas.Visibility = isVisibleCanvas ? Visibility.Visible : Visibility.Collapsed;

            if (detectedObjects != null && detectedObjects.Any())
            {
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
                            EnableRemove = enableRemoveOption
                        }
                    };

                    editor.RegionChanged += OnRegionChanged;
                    editor.RegionDeleted += OnRegionDeleted;

                    this.editObjectVisualizationCanvas.Children.Add(editor);
                }
            }
        }

        public void ShowNewObjects(string name)
        {
            foreach (var item in AddedNewObjects)
            {
                item.DisplayName = name;
            }
            this.ToggleEditState(AddedNewObjects, true);
        }

        private void OnRegionSelected(object sender, Tuple<RegionState, ProductItemViewModel> item)
        {
            if (item.Item1 == RegionState.Selected)
            {
                SelectedRegions.Add(item.Item2);
            }
            else
            {
                bool isRemoved = SelectedRegions.Remove(item.Item2);
            }
            this.RegionSelected?.Invoke(this, item);
        }

        public void ClearSelectedRegions()
        {
            if (SelectedRegions.Any())
            {
                foreach (ObjectRegionControl region in objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().ToList())
                {
                    if (SelectedRegions.Any(x => x.Id == region.ProductItemViewModel.Id))
                    {
                        region.State = RegionState.Active;
                    }
                }
                SelectedRegions.Clear();
            }
        }

        public void UnSelectRegion(ProductItemViewModel item)
        {
            var objectRegion = objectDetectionVisualizationCanvas.Children.Cast<ObjectRegionControl>().FirstOrDefault(x => x.ProductItemViewModel.Id == item.Id);
            if (objectRegion != null && objectRegion.State == RegionState.Selected)
            {
                objectRegion.State = RegionState.Active;
                SelectedRegions.Remove(item);
            }
        }

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

        private Color GetObjectRegionColor(PredictionModel prediction)
        {
            double minHigh = 0.6;
            double minMed = 0.3;
            double prob = prediction.Probability;

            if (prob >= minHigh)
            {
                return Color.FromArgb(255, 36, 143, 255);
            }
            else if (prob < minMed)
            {
                return Color.FromArgb(255, 228, 19, 35);
            }

            return Color.FromArgb(255, 250, 190, 20);
        }

        private void OnPointerReleasedOverImage(object sender, PointerRoutedEventArgs e)
        {
            bool isAnyRegionsOnCanvas = this.editObjectVisualizationCanvas.Children.Any();
            if (enableRemoveMode && !isAnyRegionsOnCanvas)
            {
                AddedNewObjects.Clear();

                var clickPosition = e.GetCurrentPoint(this.editObjectVisualizationCanvas);

                double normalizedPosX = clickPosition.Position.X / editObjectVisualizationCanvas.ActualWidth;
                double normalizedPosY = clickPosition.Position.Y / editObjectVisualizationCanvas.ActualHeight;
                double normalizedWidth = 50 / editObjectVisualizationCanvas.ActualWidth;
                double normalizedHeight = 50 / editObjectVisualizationCanvas.ActualHeight;

                PredictionModel obj = new PredictionModel(
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
                    EnableRemove = true
                }
            };

            editor.RegionChanged += OnRegionChanged;
            editor.RegionDeleted += OnRegionDeleted;

            this.editObjectVisualizationCanvas.Children.Add(editor);
            AddedNewObjects.Add(product);
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
                }

                bool isRemoved = this.editObjectVisualizationCanvas.Children.Remove(regionControl);
                if (isRemoved)
                {
                    regionControl.RegionChanged -= OnRegionChanged;
                    regionControl.RegionDeleted -= OnRegionDeleted;
                }
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
    }
}
