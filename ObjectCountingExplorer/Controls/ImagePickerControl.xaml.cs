using ObjectCountingExplorer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace ObjectCountingExplorer.Controls
{
    public enum ImagePickerState
    {
        InputTypes,
        CameraStream,
        LocalFile
    }

    public sealed partial class ImagePickerControl : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<StorageFile> OnImageSearchCompleted;
        public event PropertyChangedEventHandler PropertyChanged;

        private ImagePickerState currentState;
        public ImagePickerState CurrentState
        {
            get { return currentState; }
            set
            {
                currentState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentState"));
            }
        }

        public void SetSuggestedImageList(params string[] imageUrls)
        {
            //validate
            imageUrls = imageUrls ?? new string[] { };

            suggestedImagesGrid.ItemsSource = imageUrls.Select(url => new ImageAnalyzer(url));

            //reset scrolling
            suggestedImagesScroll.ResetScroll();
        }

        public void SetSuggestedImageList(IEnumerable<ImageSource> images)
        {
            //validate
            images = images ?? new ImageSource[] { };

            suggestedImagesGrid.ItemsSource = images.Select(i => new ImageAnalyzer((i as Windows.UI.Xaml.Media.Imaging.BitmapImage).UriSource.AbsoluteUri));

            //reset scrolling
            suggestedImagesScroll.ResetScroll();
        }

        public ImagePickerControl()
        {
            this.InitializeComponent();

            inputSourcesGridView.ItemsSource = new[]
            {
                new { Gliph = "\uF12B", Label = "From local file", Tag = ImagePickerState.LocalFile, IsWide = true },
                new { Gliph = "\uE722", Label = "From camera", Tag = ImagePickerState.CameraStream, IsWide = true }
            };

            DataContext = this;
        }

        private async Task OpenFilePickerDialogAsync()
        {
            try
            {
                FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary, ViewMode = PickerViewMode.Thumbnail };
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".jpeg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.FileTypeFilter.Add(".bmp");

                var singleFile = await fileOpenPicker.PickSingleFileAsync();
                if (singleFile != null)
                {
                    StorageFile imageFile = await singleFile.CopyAsync(ApplicationData.Current.LocalFolder, singleFile.Name, NameCollisionOption.ReplaceExisting);

                    ProcessImageSelection(imageFile);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message, "Failure processing local images").ShowAsync();
            }
        }

        private async void OnInputTypeItemClicked(object sender, ItemClickEventArgs e)
        {
            dynamic targetMode = ((dynamic)e.ClickedItem).Tag;

            if (targetMode == ImagePickerState.LocalFile)
            {
                await OpenFilePickerDialogAsync();
            }
            else if (targetMode == ImagePickerState.CameraStream)
            {
                CurrentState = ImagePickerState.CameraStream;
                await cameraControl.StartStreamAsync();
            }
            else
            {
                CurrentState = (ImagePickerState)targetMode;
            }
        }

        private async void OnBackToInputSelectionClicked(object sender, RoutedEventArgs e)
        {
            CurrentState = ImagePickerState.InputTypes;

            // make sure camera stops in case it was up
            await cameraControl.StopStreamAsync();
        }

        private void ProcessImageSelection(StorageFile file)
        {
            this.OnImageSearchCompleted?.Invoke(this, file);
            CurrentState = ImagePickerState.InputTypes;
        }

        private async void OnCameraPhotoCaptured(object sender, ImageAnalyzer img)
        {
            StorageFile imageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Image.jpg", CreationCollisionOption.ReplaceExisting);

            if (img.ImageUrl != null)
            {
                await Util.DownloadAndSaveBitmapAsync(img.ImageUrl, imageFile);
            }
            else if (img.GetImageStreamCallback != null)
            {
                await Util.SaveBitmapToStorageFileAsync(await img.GetImageStreamCallback(), imageFile);
            }

            ProcessImageSelection(imageFile);

            await this.cameraControl.StopStreamAsync();
        }

        private void OnCameraAvailableSpaceChanged(object sender, SizeChangedEventArgs e)
        {
            double aspectRatio = (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);

            double desiredHeight = this.webCamHostGridParent.ActualWidth / aspectRatio;

            if (desiredHeight > this.webCamHostGridParent.ActualHeight)
            {
                // optimize for height
                this.webCamHostGrid.Height = this.webCamHostGridParent.ActualHeight;
                this.webCamHostGrid.Width = this.webCamHostGridParent.ActualHeight * aspectRatio;
            }
            else
            {
                // optimize for width
                this.webCamHostGrid.Height = desiredHeight;
                this.webCamHostGrid.Width = this.webCamHostGridParent.ActualWidth;
            }
        }

        private async void OnImageItemClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ImageAnalyzer img)
            {
                StorageFile imageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Image.jpg", CreationCollisionOption.ReplaceExisting);

                if (img.ImageUrl != null)
                {
                    if (img.ImageUrl.Contains("ms-appx"))
                    {
                        var projectFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(img.ImageUrl));
                        imageFile = await projectFile.CopyAsync(ApplicationData.Current.LocalFolder, projectFile.Name, NameCollisionOption.ReplaceExisting);
                    }
                    else
                    {
                        await Util.DownloadAndSaveBitmapAsync(img.ImageUrl, imageFile);
                    }
                }
                else if (img.GetImageStreamCallback != null)
                {
                    await Util.SaveBitmapToStorageFileAsync(await img.GetImageStreamCallback(), imageFile);
                }

                ProcessImageSelection(imageFile);
            }
        }
    }

    public class WideStyleSelector : StyleSelector
    {
        public Style WideStyle { get; set; }
        public Style DefaultStyle { get; set; }
        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            if (((dynamic)item).IsWide)
            {
                return WideStyle;
            }
            return DefaultStyle;
        }
    }
}
