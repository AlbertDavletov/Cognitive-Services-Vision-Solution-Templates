using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

        public static async Task<ImagePrediction> PredictImageUrlWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, string publishedName, ImageUrl imageUrl)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.DetectImageUrlAsync(projectId, publishedName, imageUrl));
        }

        public static async Task<ImagePrediction> PredictImageWithRetryAsync(this ICustomVisionPredictionClient predictionApi, Guid projectId, string publishedName, Stream imageStream)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError(async () => await predictionApi.DetectImageAsync(projectId, publishedName, imageStream));
        }
    }
}
