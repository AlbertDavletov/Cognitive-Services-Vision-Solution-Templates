using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
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

    public enum SummaryViewState
    {
        GroupedByCategory,
        CategorySelected,
        GroupedByTag,
        TagSelected
    }

    public enum FilterType
    {
        ProductName,
        LowConfidence,
        MediumConfidence,
        HighConfidence
    }

    public class ProductFilter : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool? isChecked = false;
        public bool? IsChecked
        {
            get { return isChecked; }
            set
            {
                this.isChecked = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
            }
        }
        public string Name { get; set; }

        public FilterType FilterType { get; set; }

        public ProductFilter(string name, FilterType filterType, bool? isChecked = false)
        {
            Name = name;
            IsChecked = isChecked;
            FilterType = filterType;
        }
    }

    public class SummaryGroupItem
    {
        public string Name { get; set; }
        public SummaryViewState State { get; set; }
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

    public class ProjectViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ProjectViewModel(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
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

        public static SolidColorBrush GetPredictionColor(PredictionModel model)
        {
            return new SolidColorBrush(Util.GetObjectRegionColor(model.Probability));
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
