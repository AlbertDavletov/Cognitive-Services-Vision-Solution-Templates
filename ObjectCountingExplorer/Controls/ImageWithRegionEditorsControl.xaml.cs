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
        private bool enableEditMode = false;
        
        private IEnumerable<ProductItemViewModel> currentDetectedObjects;

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
                this.ShowObjectDetectionBoxes(currentDetectedObjects);
            }
        }

        public void ShowObjectDetectionBoxes(IEnumerable<ProductItemViewModel> detectedObjects)
        {
            this.cropImageButton.IsEnabled = false;
            currentDetectedObjects = detectedObjects;
            this.objectDetectionVisualizationCanvas.Children.Clear();

            double canvasWidth = objectDetectionVisualizationCanvas.ActualWidth;
            double canvasHeight = objectDetectionVisualizationCanvas.ActualHeight;

            foreach (var obj in detectedObjects)
            {
                bool isSelected = SelectedRegions.Any() ? SelectedRegions.Any(x => x.Id == obj.Id) : false;

                var model = obj.Model;
                var state = model.Probability > 0.6 ? RegionState.Active : RegionState.Disabled;
                var region = new ObjectRegionControl
                {
                    Margin = new Thickness(model.BoundingBox.Left * canvasWidth,
                                               model.BoundingBox.Top * canvasHeight, 0, 0),
                    Width = model.BoundingBox.Width * canvasWidth,
                    Height = model.BoundingBox.Height * canvasHeight,

                    Title = model.TagName,
                    State = isSelected ? RegionState.Selected : state,
                    ProductItemViewModel = obj,
                    Color = GetObjectRegionColor(model)
                };

                region.RegionSelected += OnRegionSelected;

                objectDetectionVisualizationCanvas.Children.Add(region);
            }
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
            if (enableEditMode)
            {
                var clickPosition = e.GetCurrentPoint(this.imageRegionsCanvas);

                double normalizedPosX = clickPosition.Position.X / imageRegionsCanvas.ActualWidth;
                double normalizedPosY = clickPosition.Position.Y / imageRegionsCanvas.ActualHeight;
                double normalizedWidth = 50 / imageRegionsCanvas.ActualWidth;
                double normalizedHeight = 50 / imageRegionsCanvas.ActualHeight;

                //ImageViewModel imageViewModel = (ImageViewModel)this.DataContext;
                //imageViewModel.AddedImageRegions.Add(newRegion);

                PredictionModel obj = new PredictionModel(
                    boundingBox: new BoundingBox(normalizedPosX, normalizedPosY,
                                        normalizedWidth + normalizedPosX > 1 ? 1 - normalizedPosX : normalizedWidth,
                                        normalizedHeight + normalizedPosY > 1 ? 1 - normalizedPosY : normalizedHeight));

                this.AddRegionToUI(obj);
            }
        }

        private void AddRegionToUI(PredictionModel obj)
        {
            var editor = new RegionEditorControl
            {
                //Width = image.ActualWidth * obj.BoundingBox.Width,
                //Height = image.ActualHeight * obj.BoundingBox.Height,
                //Margin = new Thickness(obj.BoundingBox.Left * image.ActualWidth, obj.BoundingBox.Top * image.ActualHeight, 0, 0),
                DataContext = new RegionEditorViewModel
                {
                    Region = obj,
                    // AvailableTags = currentProject.Tags,
                    Color = Colors.Lime
                }
            };

            editor.RegionChanged += OnRegionChanged;
            editor.RegionDeleted += OnRegionDeleted;

            this.imageRegionsCanvas.Children.Add(editor);
        }

        private void OnRegionChanged(object sender, Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Tag tag)
        {
            RegionEditorControl regionControl = (RegionEditorControl)sender;

            RegionEditorViewModel regionEditorViewModel = (RegionEditorViewModel)regionControl.DataContext;
        }

        private void OnRegionDeleted(object sender, EventArgs e)
        {
            RegionEditorControl regionControl = (RegionEditorControl)sender;
            this.imageRegionsCanvas.Children.Remove(regionControl);
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
