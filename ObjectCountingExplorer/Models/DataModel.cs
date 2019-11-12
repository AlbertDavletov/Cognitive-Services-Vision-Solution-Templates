using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Media;

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
        public Guid Id { get; private set; }
        public PredictionModel Model { get; private set; }


        public string Name { get; set; }
        public ImageSource Image { get; set; }
        public Rect Rect { get; set; }

        public ProductItemViewModel(PredictionModel model)
        {
            Id = Guid.NewGuid();
            Model = model;

            Name = model.TagName;
            Rect = new Rect(model.BoundingBox.Left, model.BoundingBox.Top, model.BoundingBox.Width, model.BoundingBox.Height);
        }
    }
}
