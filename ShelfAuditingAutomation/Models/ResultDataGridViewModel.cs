using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ShelfAuditingAutomation.Models
{
    public class ResultDataGridViewModel
    {
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
    }
}
