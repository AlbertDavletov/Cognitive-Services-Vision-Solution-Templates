using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Rest;
using ShelfAuditingAutomation.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ShelfAuditingAutomation
{
    internal static class Util
    {
        public const string RequiredFieldMessage = "This field is required";
        public const string ShelfGapName = "Gap";
        public const string UnknownProductName = "Product";

        public static readonly Uri GapImageUrl = new Uri("ms-appx:///Assets/ProductSamples/gap.jpg");
        public static readonly Uri UnknownProductImageUri = new Uri("ms-appx:///Assets/ProductSamples/product.jpg");

        public static readonly Color TaggedItemColor =      Color.FromArgb(255, 36, 143, 255); // #248FFF
        public static readonly Color ShelfGapColor =        Color.FromArgb(255, 250, 190, 20); // #FABE14
        public static readonly Color UnknownProductColor =  Color.FromArgb(255, 180, 0, 158);  // #B4009E

        public static Color GetObjectRegionColor(PredictionModel model)
        {
            if (model?.TagName != null && model.TagName.Equals(UnknownProductName, StringComparison.OrdinalIgnoreCase))
            {
                return UnknownProductColor;
            }
            else if (model?.TagName != null && model.TagName.Equals(ShelfGapName, StringComparison.OrdinalIgnoreCase))
            {
                return ShelfGapColor;
            }

            return TaggedItemColor;
        }

        public static async Task<bool> CheckAssetsFile(string fileName)
        {
            // Get the path to the app's Assets folder.
            string root = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            string path = root + @"\Assets\ProductSamples";

            // Get the app's Assets folder.
            StorageFolder assetsFolder = await StorageFolder.GetFolderFromPathAsync(path);

            // Check whether an image with the specified scale exists.
            return await assetsFolder.TryGetItemAsync(fileName) != null;
        }

        public static double EnsureValidNormalizedValue(double value)
        {
            // ensure [0,1]
            return Math.Min(1, Math.Max(0, value));
        }

        public static bool IsLowConfidenceRegion(PredictionModel model)
        {
            return model?.Probability <= SettingsHelper.Instance.LowConfidence;
        }

        internal static async Task GenericApiCallExceptionHandler(Exception ex, string errorTitle)
        {
            string errorDetails = GetMessageFromException(ex);

            await new MessageDialog(errorDetails, errorTitle).ShowAsync();
        }

        internal static string GetMessageFromException(Exception ex)
        {
            string errorDetails = ex.Message;

            HttpOperationException httpException = ex as HttpOperationException;
            if (httpException?.Response?.ReasonPhrase != null)
            {
                string errorReason = $"\"{httpException.Response.ReasonPhrase}\".";
                if (httpException?.Response?.Content != null)
                {
                    errorReason += $" Some more details: {httpException.Response.Content}";
                }

                errorDetails = $"{ex.Message}. The error was {errorReason}.";
            }

            return errorDetails;
        }

        internal static async Task<byte[]> GetPixelBytesFromSoftwareBitmapAsync(SoftwareBitmap softwareBitmap)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();

                // Read the pixel bytes from the memory stream
                using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                {
                    var bytes = new byte[stream.Size];
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                    return bytes;
                }
            }
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }

        public static double NormalizeValue(double value, double min, double max)
        {
            return (value - min) / (max - min);
        }

        public static double GetScaledValue(double scale, double minValue, double maxValue)
        {
            double newValue = minValue + (1 - scale) * (maxValue - minValue);
            if (newValue < minValue)
            {
                return minValue;
            }
            else if (newValue > maxValue)
            {
                return maxValue;
            }
            return newValue;
        }

        public static async Task<StorageFile> PickSingleFileAsync(string[] fileTypeFilter)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary, ViewMode = PickerViewMode.Thumbnail };
            fileTypeFilter.ToList().ForEach(f => fileOpenPicker.FileTypeFilter.Add(f));
            return await fileOpenPicker.PickSingleFileAsync();
        }

        public static ImageSource GetCanonicalImage(string baseUrl, string tagName)
        {
            string name = tagName.ToLower();
            string uri = Uri.EscapeUriString($"{baseUrl}{name}.jpg");
            var bitmap = new BitmapImage();

            bool isUri = !string.IsNullOrEmpty(uri) ? Uri.IsWellFormedUriString(uri, UriKind.Absolute) : false;
            if (!isUri)
            {
                bitmap.UriSource = name.Equals("gap", StringComparison.OrdinalIgnoreCase) ? GapImageUrl : UnknownProductImageUri;
                return bitmap;
            }

            bitmap.ImageFailed += (s, e) => bitmap.UriSource = name.Equals("gap", StringComparison.OrdinalIgnoreCase) ? GapImageUrl : UnknownProductImageUri;
            bitmap.UriSource = new Uri(uri);

            return bitmap;
        }

        public static async Task DownloadAndSaveBitmapAsync(string imageUrl, StorageFile resultFile)
        {
            byte[] imgBytes = await new System.Net.Http.HttpClient().GetByteArrayAsync(imageUrl);
            using (Stream stream = new MemoryStream(imgBytes))
            {
                await SaveBitmapToStorageFileAsync(stream, resultFile);
            }
        }

        public static async Task SaveBitmapToStorageFileAsync(Stream localFileStream, StorageFile resultFile)
        {
            // Get pixels
            var pixels = await GetPixelsAsync(localFileStream.AsRandomAccessStream());

            // Save result to new image
            using (Stream resultStream = await resultFile.OpenStreamForWriteAsync())
            {
                IRandomAccessStream randomAccessStream = resultStream.AsRandomAccessStream();
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                        BitmapAlphaMode.Ignore,
                                        pixels.Item2.ScaledWidth, pixels.Item2.ScaledHeight,
                                        DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, pixels.Item1);

                await encoder.FlushAsync();
            }
        }

        private static async Task<Tuple<byte[], BitmapTransform>> GetPixelsAsync(IRandomAccessStream stream)
        {
            // Create a decoder from the stream. With the decoder, we can get the properties of the image.
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // Create BitmapTransform and define the bounds.
            BitmapTransform transform = new BitmapTransform
            {
                ScaledHeight = decoder.PixelHeight,
                ScaledWidth = decoder.PixelWidth
            };

            // Get the cropped pixels within the bounds of transform. 
            PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            return new Tuple<byte[], BitmapTransform>(pix.DetachPixelData(), transform);
        }

        public static async Task<ImageSource> GetCroppedBitmapAsync(IRandomAccessStream stream, Rect rectangle)
        {
            var pixels = await GetCroppedPixelsAsync(stream, rectangle);

            // Stream the bytes into a WriteableBitmap 
            WriteableBitmap cropBmp = new WriteableBitmap((int)pixels.Item2.Width, (int)pixels.Item2.Height);
            cropBmp.FromByteArray(pixels.Item1);

            return cropBmp;
        }

        private static async Task<Tuple<byte[], BitmapBounds>> GetCroppedPixelsAsync(IRandomAccessStream stream, Rect rectangle)
        {
            // Create a decoder from the stream. With the decoder, we can get  
            // the properties of the image. 
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            // Create cropping BitmapTransform and define the bounds. 
            BitmapTransform transform = new BitmapTransform();
            BitmapBounds bounds = new BitmapBounds
            {
                X = Math.Max(0, (uint)rectangle.Left),
                Y = Math.Max(0, (uint)rectangle.Top)
            };
            bounds.Height = bounds.Y + rectangle.Height <= decoder.PixelHeight ? (uint)rectangle.Height : decoder.PixelHeight - bounds.Y;
            bounds.Width = bounds.X + rectangle.Width <= decoder.PixelWidth ? (uint)rectangle.Width : decoder.PixelWidth - bounds.X;
            transform.Bounds = bounds;

            // Get the cropped pixels within the bounds of transform. 
            PixelDataProvider pix = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            return new Tuple<byte[], BitmapBounds>(pix.DetachPixelData(), transform.Bounds);
        }
    }
}
