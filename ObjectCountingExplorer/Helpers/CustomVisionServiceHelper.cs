using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using System.Linq;
using System.Collections.ObjectModel;
using ObjectCountingExplorer.Models;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace ObjectCountingExplorer.Helpers
{
    public static class CustomVisionServiceHelper
    {
        public static readonly List<Guid> ObjectDetectionDomainGuidList = new List<Guid>()
        {
            new Guid("da2e3a8a-40a5-4171-82f4-58522f70fbc1"), // Object Detection, General
            new Guid("1d8ffafe-ec40-4fb2-8f90-72b3b6cecea4"), // Object Detection, Logo
            new Guid("a27d5ca5-bb19-49d8-a70a-fec086c47f5b")  // Object Detection, General (exportable)
        };
        public static int RetryCountOnQuotaLimitError = 6;
        public static int RetryDelayOnQuotaLimitError = 500;

        public static async Task<ImagePrediction> AnalyzeImageAsync(ICustomVisionTrainingClient trainingApi, ICustomVisionPredictionClient predictionApi, Guid projectId, StorageFile file)
        {
            ImagePrediction result = null;

            try
            {
                var iteractions = await trainingApi.GetIterationsAsync(projectId);
                var latestTrainedIteraction = iteractions.Where(i => i.Status == "Completed").OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();
                if (latestTrainedIteraction == null || string.IsNullOrEmpty(latestTrainedIteraction?.PublishName))
                {
                    throw new Exception("This project doesn't have any trained models or published iteration yet. Please train and publish it, or wait until training completes if one is in progress.");
                }

                using (Stream stream = (await file.OpenReadAsync()).AsStream())
                {
                    result = await PredictImageWithRetryAsync(predictionApi, projectId, latestTrainedIteraction.PublishName, stream);
                }
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Custom Vision service error");
            }

            return result;
        }

        public static async void PopulateTagSamplesAsync(ICustomVisionTrainingClient trainingApi, Guid projectId, ObservableCollection<TagSampleViewModel> collection)
        {
            var tags = (await trainingApi.GetTagsAsync(projectId)).Take(5);
            foreach (var tag in tags.OrderBy(t => t.Name))
            {
                try
                {
                    if (tag.ImageCount > 0)
                    {
                        var imageModelSample = (await trainingApi.GetTaggedImagesAsync(projectId, null, new List<Guid>() { tag.Id }, null, 1)).First();

                        var tagRegion = imageModelSample.Regions?.FirstOrDefault(r => r.TagId == tag.Id);
                        if (tagRegion == null || (tagRegion.Width == 0 && tagRegion.Height == 0))
                        {
                            collection.Add(new TagSampleViewModel { TagName = tag.Name, TagSampleImage = new BitmapImage(new Uri(imageModelSample.ThumbnailUri)) });
                        }
                        else
                        {
                            // Crop a region from the image that is associated with the tag, so we show something more 
                            // relevant than the whole image. 
                            ImageSource croppedImage = await Util.DownloadAndCropBitmapAsync(
                                imageModelSample.OriginalImageUri,
                                new Rect(
                                    tagRegion.Left * imageModelSample.Width,
                                    tagRegion.Top * imageModelSample.Height,
                                    tagRegion.Width * imageModelSample.Width,
                                    tagRegion.Height * imageModelSample.Height));

                            collection.Add(new TagSampleViewModel { TagName = tag.Name, TagSampleImage = croppedImage });
                        }
                    }
                }
                catch (HttpOperationException exception) when (exception.Response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    continue;
                }
            }
        }

        public static async Task<ImagePrediction> PredictImageUrlWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, string publishedName, ImageUrl imageUrl)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.DetectImageUrlAsync(projectId, publishedName, imageUrl));
        }

        public static async Task<ImagePrediction> PredictImageWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, string publishedName, Stream imageStream)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.DetectImageAsync(projectId, publishedName, imageStream));
        }

        private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = RetryCountOnQuotaLimitError;
            int delay = RetryDelayOnQuotaLimitError;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (HttpOperationException exception) when (exception.Response.StatusCode == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        public static List<PredictionModel> GetFakeTestData(double tempW, double tempH, double marginX, double marginY)
        {
            return new List<PredictionModel>()
            {
                new PredictionModel(probability: 0.99,  tagName: "General Mills", boundingBox: new BoundingBox(marginX, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.98,  tagName: "General Mills", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.97,  tagName: "General Mills", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.96,  tagName: "General Mills", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.95,  tagName: "General Mills", boundingBox: new BoundingBox(5 * marginX + 4 * tempW, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.94,  tagName: "General Mills", boundingBox: new BoundingBox(6 * marginX + 5 * tempW, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.6,   tagName: "General Mills", boundingBox: new BoundingBox(7 * marginX + 6 * tempW, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.59,  tagName: "General Mills", boundingBox: new BoundingBox(8 * marginX + 7 * tempW, marginY, tempW, tempH)),
                new PredictionModel(probability: 0.25,  tagName: "General Mills", boundingBox: new BoundingBox(9 * marginX + 8 * tempW, marginY, tempW, tempH)),


                new PredictionModel(probability: 0.81,  tagName: "Great Value", boundingBox: new BoundingBox(marginX, 2 * marginY + tempH, tempW, tempH)),
                new PredictionModel(probability: 0.79,  tagName: "Great Value", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 2 * marginY + tempH, tempW, tempH)),
                new PredictionModel(probability: 0.78,  tagName: "Great Value", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 2 * marginY + tempH, tempW, tempH)),
                new PredictionModel(probability: 0.59,  tagName: "Great Value", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, 2 * marginY + tempH, tempW, tempH)),
                new PredictionModel(probability: 0.58,  tagName: "Great Value", boundingBox: new BoundingBox(5 * marginX + 4 * tempW, 2 * marginY + tempH, tempW, tempH)),


                new PredictionModel(probability: 0.99, tagName: "Quaker", boundingBox: new BoundingBox(marginX, 3 * marginY + 2 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.98, tagName: "Quaker", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 3 * marginY + 2 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.97, tagName: "Quaker", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 3 * marginY + 2 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.96, tagName: "Quaker", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, 3 * marginY + 2 * tempH, tempW, tempH)),


                new PredictionModel(probability: 0.8,  tagName: "Kellog", boundingBox: new BoundingBox(marginX, 4 * marginY + 3 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.81, tagName: "Kellog", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 4 * marginY + 3 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.82, tagName: "Kellog", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 4 * marginY + 3 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.7,  tagName: "Kellog", boundingBox: new BoundingBox(4 * marginX + 3 * tempW, 4 * marginY + 3 * tempH, tempW, tempH)),


                new PredictionModel(probability: 0.8,  tagName: "None", boundingBox: new BoundingBox(marginX, 5 * marginY + 4 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.81, tagName: "None", boundingBox: new BoundingBox(2 * marginX + 1 * tempW, 5 * marginY + 4 * tempH, tempW, tempH)),
                new PredictionModel(probability: 0.82, tagName: "None", boundingBox: new BoundingBox(3 * marginX + 2 * tempW, 5 * marginY + 4 * tempH, tempW, tempH))
            };
        }
    }
}
