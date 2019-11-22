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

        public static List<PredictionModel> GetFakeTestData()
        {
            return new List<PredictionModel>()
            {
                new PredictionModel(probability: 0.99,  tagName: "Trop50 Orange Juice", boundingBox: new BoundingBox(0.135476189113207, 0.669977965318636, 0.17846288184557, 0.308048642493458)),
                new PredictionModel(probability: 0.98,  tagName: "Trop50 Orange Juice", boundingBox: new BoundingBox(0.306554068727999, 0.681500409919821, 0.151975687560846, 0.310943845358046)),
                new PredictionModel(probability: 0.97,  tagName: "Trop50 Orange Juice", boundingBox: new BoundingBox(0.464153883968774, 0.686719870097959, 0.147633526762836, 0.303416329837948)),

                new PredictionModel(probability: 0.79,  tagName: "Tropicana Orange Juice", boundingBox: new BoundingBox(0.612670091605608, 0.686178346532142, 0.156752062947892, 0.303995366434922)),
                new PredictionModel(probability: 0.78,  tagName: "Tropicana Orange Juice", boundingBox: new BoundingBox(0.764694124616114, 0.693662981496752, 0.149804599708025, 0.29415170452693)),
                new PredictionModel(probability: 0.59,  tagName: "Tropicana Orange Juice", boundingBox: new BoundingBox(0.894931667590343, 0.7098632036725, 0.105068332409657, 0.269832087935157)),

                new PredictionModel(probability: 0.99, tagName: "Minute Maid Orange Juice", boundingBox: new BoundingBox(0.067717112064961, 0.0486046126516127, 0.269214058922355, 0.569774198191264)),
                new PredictionModel(probability: 0.8, tagName: "Minute Maid Orange Juice", boundingBox: new BoundingBox(0.290491244628631, 0.0491955075014134, 0.224924021764188, 0.570353214908518)),
                new PredictionModel(probability: 0.5, tagName: "Minute Maid Orange Juice", boundingBox: new BoundingBox(0.498494925501794, 0.0538241821682095, 0.240990021189111, 0.567458012043929)),
                new PredictionModel(probability: 0.2, tagName: "Minute Maid Orange Juice", boundingBox: new BoundingBox(0.676108623921835, 0.0694828807781399, 0.299174999734644, 0.554140150433815)),

                new PredictionModel(probability: 0.8,  tagName: "None", boundingBox: new BoundingBox(0.00257976566872595, 0.707548965737701, 0.14893617052995, 0.290098448348115)),
                new PredictionModel(probability: 0.81, tagName: "None", boundingBox: new BoundingBox(0.856270454016098, 0.296435049377248, 0.143725580553865, 0.331210205771003)),
                new PredictionModel(probability: 0.82, tagName: "None", boundingBox: new BoundingBox(0, 0.0578619321656251, 0.160660009156864, 0.561088589597499))
            };
        }
    }
}
