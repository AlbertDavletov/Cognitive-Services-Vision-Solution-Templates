using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using TrainingModels = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;

namespace ObjectCountingExplorer.Models
{
    public class ProductFilter : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isChecked = false;
        public bool IsChecked
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

        public ProductFilter(string name, FilterType filterType, bool isChecked = false)
        {
            Name = name;
            IsChecked = isChecked;
            FilterType = filterType;
        }
    }

    public class ProductTag
    {
        public TrainingModels.Tag Tag { get; set; }
        public ProductTag(TrainingModels.Tag tag)
        {
            this.Tag = tag;
        }

        public static ImageSource GetTagImageSource(TrainingModels.Tag tag)
        {
            string tagName = tag.Name.ToLower();
            bool isTagImageExist = Task.Run(() => Util.CheckAssetsFile($"{tagName}.jpg")).Result;
            string tagImageName = isTagImageExist ? $"{tagName}.jpg" : "product.jpg";
            return new BitmapImage(new Uri($"ms-appx:///Assets/ProductSamples/{tagImageName}"));
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
            return new SolidColorBrush(Util.GetObjectRegionColor(model));
        }
    }

    public class ResultDataGridViewModel
    {
        public string Name { get; set; }
        public int TotalCount { get; set; }
        public bool IsAggregateColumn { get; set; } = false;
    }

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
        Edit,
        Collapsed
    }

    public enum SummaryViewState
    {
        GroupedByCategory,
        GroupedByTag,
        SelectedItems
    }

    public enum FilterType
    {
        ProductName,
        LowConfidence,
        MediumConfidence,
        HighConfidence,
        UnknownProduct,
        ShelfGap
    }

    public enum EditorState
    {
        Add,
        Edit
    }

    public enum UpdateStatus
    {
        UpdateNewProduct,
        UpdateExistingProduct,
        SaveNewProduct,
        SaveExistingProduct
    }
}
