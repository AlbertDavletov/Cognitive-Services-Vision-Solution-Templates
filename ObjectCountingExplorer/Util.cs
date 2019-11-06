using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

namespace ObjectCountingExplorer
{
    internal static class Util
    {
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

        public static List<Color> GetColors(int count)
        {
            var colors = new List<Color>();

            var colorNames = new List<Color>();
            foreach (var color in typeof(Colors).GetRuntimeProperties())
            {
                colorNames.Add((Color)color.GetValue(null));
            }

            List<int> randomUniqueNumbers = GetRandomUniqueNumbers(count, 0, colorNames.Count);

            foreach (int number in randomUniqueNumbers)
            {
                colors.Add(colorNames[number]);
            }

            return colors;
        }

        public static List<int> GetRandomUniqueNumbers(int count, int start, int end)
        {
            if (count > end - start)
            {
                throw new ArgumentException("Count should be less then input range.");
            }

            Random rnd = new Random();
            List<int> randomList = new List<int>();

            while (randomList.Count < count)
            {
                int number = rnd.Next(start, end);
                if (!randomList.Contains(number))
                {
                    randomList.Add(number);
                }
            }

            return randomList;
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
    }
}
