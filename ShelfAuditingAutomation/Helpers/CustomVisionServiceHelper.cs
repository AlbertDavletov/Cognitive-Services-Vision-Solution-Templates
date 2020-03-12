using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Rest;
using ShelfAuditingAutomation.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

namespace ShelfAuditingAutomation.Helpers
{
    public static class CustomVisionServiceHelper
    {
        public const int MaxRegionsInBatch = 64;
        public const int RetryCountOnQuotaLimitError = 6;
        public const int RetryDelayOnQuotaLimitError = 500;

        public static async Task<ImagePrediction> AnalyzeImageAsync(ICustomVisionTrainingClient trainingApi, ICustomVisionPredictionClient predictionApi, Guid projectId, StorageFile file)
        {
            ImagePrediction result = null;

            try
            {
                var iteractions = await trainingApi.GetIterationsAsync(projectId);
                var latestTrainedIteraction = iteractions.Where(i => i.Status == "Completed" && !string.IsNullOrEmpty(i.PublishName)).OrderByDescending(i => i.TrainedAt.Value).FirstOrDefault();
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

        public static async Task<IEnumerable<TrainingModels.Tag>> GetTagsAsync(ICustomVisionTrainingClient trainingApi, Guid projectId)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await trainingApi.GetTagsAsync(projectId));
        }

        public static async Task<ImagePrediction> PredictImageUrlWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, string publishedName, ImageUrl imageUrl)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.DetectImageUrlAsync(projectId, publishedName, imageUrl));
        }

        public static async Task<ImagePrediction> PredictImageWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, string publishedName, Stream imageStream)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.DetectImageAsync(projectId, publishedName, imageStream));
        }

        public static async Task AddImageRegionsAsync(ICustomVisionTrainingClient trainingApi, Guid projectId, StorageFile file, List<ProductItemViewModel> items)
        {
            var regions = items?.Select(p => p.Model).ToList();
            if (regions.Any())
            {
                TrainingModels.ImageCreateSummary addResult = await CreateImageFromFileAsync(trainingApi, projectId, file);

                var addedImage = addResult?.Images?.FirstOrDefault();
                if (addedImage != null && addedImage.Status == "OKDuplicate")
                {
                    await trainingApi.DeleteImagesAsync(projectId, new List<Guid>() { addedImage.Image.Id });

                    addResult = await CreateImageFromFileAsync(trainingApi, projectId, file);
                    addedImage = addResult?.Images?.FirstOrDefault();
                }

                if (addedImage != null)
                {
                    for (int k = 0; k < regions.Count; k += MaxRegionsInBatch)
                    {
                        var curRegions = regions.Skip(k).Take(MaxRegionsInBatch);

                        // add any new regions to the image 
                        var result = await trainingApi.CreateImageRegionsAsync(projectId,
                            new TrainingModels.ImageRegionCreateBatch(
                                curRegions.Select(r => new TrainingModels.ImageRegionCreateEntry(
                                    addedImage.Image.Id, r.TagId, r.BoundingBox.Left, r.BoundingBox.Top, r.BoundingBox.Width, r.BoundingBox.Height)).ToArray())
                        );
                    }
                }
            }
        }

        private static async Task<TrainingModels.ImageCreateSummary> CreateImageFromFileAsync(ICustomVisionTrainingClient trainingApi, Guid projectId, StorageFile file)
        {
            TrainingModels.ImageCreateSummary addResult;
            using (Stream stream = (await file.OpenReadAsync()).AsStream())
            {
                addResult = await trainingApi.CreateImagesFromDataAsync(projectId, stream);
            }
            return addResult;
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
    }
}
