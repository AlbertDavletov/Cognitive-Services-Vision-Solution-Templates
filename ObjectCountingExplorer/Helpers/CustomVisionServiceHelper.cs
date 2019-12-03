using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Rest;
using ObjectCountingExplorer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

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
                TrainingModels.ImageCreateSummary addResult;
                using (Stream stream = (await file.OpenReadAsync()).AsStream())
                {
                    addResult = await trainingApi.CreateImagesFromDataAsync(projectId, stream);
                }

                var addedImage = addResult?.Images?.FirstOrDefault();
                if (addedImage != null)
                {
                    // add any new regions to the image 
                    await trainingApi.CreateImageRegionsAsync(projectId,
                        new TrainingModels.ImageRegionCreateBatch(
                            regions.Select(r => new TrainingModels.ImageRegionCreateEntry(
                                addedImage.Image.Id, r.TagId, r.BoundingBox.Left, r.BoundingBox.Top, r.BoundingBox.Width, r.BoundingBox.Height)).ToArray())
                    );

                }
            }
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
                new PredictionModel(probability: 0.99, tagName: "Trop50 Orange Juice", tagId: new Guid("a193d35f-e276-4c8f-b309-56bac624d05f"), boundingBox: new BoundingBox(0.135476189113207, 0.669977965318636, 0.17846288184557, 0.308048642493458)),
                new PredictionModel(probability: 0.98, tagName: "Trop50 Orange Juice", tagId: new Guid("9fe2fa62-1824-4d63-a146-d38fb3052ec6"), boundingBox: new BoundingBox(0.306554068727999, 0.681500409919821, 0.151975687560846, 0.310943845358046)),
                new PredictionModel(probability: 0.97, tagName: "Trop50 Orange Juice", tagId: new Guid("44a96011-349d-444a-9ee0-ea6f4b47aa67"), boundingBox: new BoundingBox(0.464153883968774, 0.686719870097959, 0.147633526762836, 0.303416329837948)),

                new PredictionModel(probability: 0.79, tagName: "Tropicana Orange Juice", tagId: new Guid("52b451f2-d132-4017-bfb8-9c30bcc4fbad"), boundingBox: new BoundingBox(0.612670091605608, 0.686178346532142, 0.156752062947892, 0.303995366434922)),
                new PredictionModel(probability: 0.78, tagName: "Tropicana Orange Juice", tagId: new Guid("f5950503-60c5-4dd9-a8e5-ad58abfc00dc"), boundingBox: new BoundingBox(0.764694124616114, 0.693662981496752, 0.149804599708025, 0.29415170452693)),
                new PredictionModel(probability: 0.59, tagName: "Tropicana Orange Juice", tagId: new Guid("aa55a6d7-17c7-426d-87d1-6e6817a0b776"), boundingBox: new BoundingBox(0.894931667590343, 0.7098632036725, 0.105068332409657, 0.269832087935157)),

                new PredictionModel(probability: 0.99, tagName: "Minute Maid Orange Juice", tagId: new Guid("993be70a-87ae-4404-86fd-666351e4b32c"), boundingBox: new BoundingBox(0.067717112064961, 0.0486046126516127, 0.269214058922355, 0.569774198191264)),
                new PredictionModel(probability: 0.8, tagName: "Minute Maid Orange Juice", tagId: new Guid("d6667b39-f867-4148-b35c-8a168424adaa"), boundingBox: new BoundingBox(0.290491244628631, 0.0491955075014134, 0.224924021764188, 0.570353214908518)),
                new PredictionModel(probability: 0.5, tagName: "Minute Maid Orange Juice", tagId: new Guid("bbd7ba78-837a-4038-849c-cedbea867bdf"), boundingBox: new BoundingBox(0.498494925501794, 0.0538241821682095, 0.240990021189111, 0.567458012043929)),
                new PredictionModel(probability: 0.2, tagName: "Minute Maid Orange Juice", tagId: new Guid("37efb514-d21c-46eb-848a-f308a2c0f28c"), boundingBox: new BoundingBox(0.676108623921835, 0.0694828807781399, 0.299174999734644, 0.554140150433815)),

                new PredictionModel(probability: 0.8,  tagName: "None", tagId: new Guid("9322ad29-ad52-42cd-81ef-751653fb569c"), boundingBox: new BoundingBox(0.00257976566872595, 0.707548965737701, 0.14893617052995, 0.290098448348115)),
                new PredictionModel(probability: 0.81, tagName: "None", tagId: new Guid("999000fd-5298-46c1-b81e-55038e8f7f64"), boundingBox: new BoundingBox(0.856270454016098, 0.296435049377248, 0.143725580553865, 0.331210205771003)),
                new PredictionModel(probability: 0.82, tagName: "None", tagId: new Guid("b4ae3955-766a-48fe-bd8b-9d799d61ce43"), boundingBox: new BoundingBox(0, 0.0578619321656251, 0.160660009156864, 0.561088589597499))
            };
        }
    }
}
