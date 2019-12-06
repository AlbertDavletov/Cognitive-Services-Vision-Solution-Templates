using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Rest;
using ObjectCountingExplorer.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ObjectCountingExplorer
{
    internal static class Util
    {
        public const string EmptyGapName = "Gap";
        public const string UnknownProductName = "Product";
        public const double MinHighProbability = 0.6;
        public const double MinMediumProbability = 0.3;

        public static readonly Color HighConfidenceColor = Color.FromArgb(255, 36, 143, 255);
        public static readonly Color MediumConfidenceColor = Color.FromArgb(255, 250, 190, 20);
        public static readonly Color LowConfidenceColor = Color.FromArgb(255, 228, 19, 35);
        public static readonly Color UnknownProductColor = Color.FromArgb(255, 180, 0, 158);
        public static readonly Color EmptyGapColor = Color.FromArgb(255, 0, 158, 179);

        public static Color GetObjectRegionColor(PredictionModel model)
        {
            double minHigh = MinHighProbability;
            double minMed = MinMediumProbability;

            if (model.TagName.Equals(UnknownProductName, StringComparison.OrdinalIgnoreCase))
            {
                return UnknownProductColor;
            }
            else if (model.TagName.Equals(EmptyGapName, StringComparison.OrdinalIgnoreCase))
            {
                return EmptyGapColor;
            }
            else if (model.Probability >= minHigh)
            {
                return HighConfidenceColor;
            }
            else if (model.Probability < minMed)
            {
                return LowConfidenceColor;
            }

            return MediumConfidenceColor;
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

        public static double Max(params double[] values)
        {
            return Enumerable.Max(values);
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

        public static async Task<ImageSource> DownloadAndCropBitmapAsync(string imageUrl, Rect rectangle)
        {
            byte[] imgBytes = await new System.Net.Http.HttpClient().GetByteArrayAsync(imageUrl);
            using (Stream stream = new MemoryStream(imgBytes))
            {
                return await GetCroppedBitmapAsync(stream.AsRandomAccessStream(), rectangle);
            }
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
