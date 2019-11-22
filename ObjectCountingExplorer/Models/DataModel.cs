﻿using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace ObjectCountingExplorer.Models
{
    public enum AppViewState
    {
        ImageSelection,
        ImageSelected,
        ImageAnalyzed,
        ImageAddOrUpdateProduct,
        ImageAnalysisReview,
        ImageAnalysisPublish
    }

    public enum RegionState
    {
        Disabled,
        Active,
        Selected,
        Edit
    }

    public class DetectedObjectsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Guid ObjectId { get; set; }
        public string ObjectName { get; set; }
        public int ObjectCount { get; set; }
        public Color ObjectColor { get; set; }

        private bool isEnable = true;
        public bool IsEnable
        {
            get { return this.isEnable; }
            set
            {
                this.isEnable = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnable"));
            }
        }
    }

    public enum RecognitionGroup
    {
        Unknown,
        Summary,
        LowConfidence,
        MediumConfidence,
        HighConfidence,
        SelectedItems
    }

    public class RecognitionGroupViewModel
    {
        public string Name { get; set; }
        public RecognitionGroup Group { get; set; }
    }

    public class ProjectViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ObservableCollection<TagSampleViewModel> TagSamples { get; set; }

        public ProjectViewModel(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class TagSampleViewModel
    {
        public string TagName { get; set; }
        public ImageSource TagSampleImage { get; set; }
    }

    public class ProductItemViewModel
    {
        public Guid Id { get; set; }
        public PredictionModel Model { get; set; }

        public string DisplayName { get; set; }

        public ImageSource Image { get; set; }

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
                Model = new PredictionModel(this.Model.Probability, this.Model.TagId, this.Model.TagName, 
                            new BoundingBox(this.Model.BoundingBox.Left, this.Model.BoundingBox.Top, 
                                            this.Model.BoundingBox.Width, this.Model.BoundingBox.Height))
            };
        }
    }

    public class ResultDataGridViewModel
    {
        public string Name { get; set; }
        public int LowConfidenceCount { get; set; }
        public int MediumConfidenceCount { get; set; }
        public int HighConfidenceCount { get; set; }
        public int TotalCount { get; set; }
    }
}
