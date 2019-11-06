using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI;

namespace ObjectCountingExplorer.Models
{
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
        HighConfidence
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
        public IList<Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Tag> Tags { get; set; }
        public ProjectViewModel(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class ProductItemViewModel
    {
        public string Name { get; set; }
        public PredictionModel Model { get; set; }
    }
}
