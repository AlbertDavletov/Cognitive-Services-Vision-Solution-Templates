using System.ComponentModel;

namespace ShelfAuditingAutomation.Models
{
    public enum FilterType
    {
        LowConfidence,
        MediumConfidence,
        HighConfidence,
        UnknownProduct,
        ShelfGap,
        ProductName
    }

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
}
