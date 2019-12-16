using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ShelfAuditingAutomation.Models
{
    public class ProductTag
    {
        public Tag Tag { get; set; }
        public ProductTag(Tag tag)
        {
            this.Tag = tag;
        }

        public static ImageSource GetTagImageSource(Tag tag)
        {
            string tagName = tag.Name.ToLower();
            bool isTagImageExist = Task.Run(() => Util.CheckAssetsFile($"{tagName}.jpg")).Result;
            string tagImageName = isTagImageExist ? $"{tagName}.jpg" : "product.jpg";
            return new BitmapImage(new Uri($"ms-appx:///Assets/ProductSamples/{tagImageName}"));
        }
    }
}
