﻿using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace ShelfAuditingAutomation.Models
{
    public class ProductItemViewModel
    {
        public Guid Id { get; set; }
        public PredictionModel Model { get; set; }
        public string DisplayName { get; set; }
        public ImageSource Image { get; set; }
        public string CanonicalImagesBaseUrl { get; set; }

        public ProductItemViewModel()
        {
            Id = Guid.NewGuid();
        }

        public ProductItemViewModel DeepCopy()
        {
            return new ProductItemViewModel()
            {
                Id = this.Id,
                DisplayName = this.DisplayName,
                Image = this.Image,
                CanonicalImagesBaseUrl = this.CanonicalImagesBaseUrl,
                Model = new PredictionModel(this.Model.Probability, this.Model.TagId, this.Model.TagName,
                            new BoundingBox(this.Model.BoundingBox.Left, this.Model.BoundingBox.Top,
                                            this.Model.BoundingBox.Width, this.Model.BoundingBox.Height))
            };
        }

        public static Visibility IsLowConfidenceItem(PredictionModel model)
        {
            return Util.IsLowConfidenceRegion(model) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
