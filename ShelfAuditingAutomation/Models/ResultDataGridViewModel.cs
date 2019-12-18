using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ShelfAuditingAutomation.Models
{
    public class ResultDataGridViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isChecked = true;
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
        public int TotalCount { get; set; }
        public int ExpectedCount { get; set; }
        public double ExpectedCoverage { get; set; }
        public bool IsAggregateColumn { get; set; } = false;

        public static ImageSource GetTagImageSource(string name)
        {
            string tagName = name.ToLower();
            bool isTagImageExist = Task.Run(() => Util.CheckAssetsFile($"{tagName}.jpg")).Result;
            string tagImageName = isTagImageExist ? $"{tagName}.jpg" : "product.jpg";
            return new BitmapImage(new Uri($"ms-appx:///Assets/ProductSamples/{tagImageName}"));
        }

        public static string GetExpectedCountValue(int expectedCount, double expectedCoverage)
        {
            string result = expectedCount >= 0 ? $"{expectedCount}" : "N/A";
            if (expectedCoverage > 0)
            {
                result += $" ({100 * expectedCoverage}%)";
            }
            return result;
        }
    }
}
