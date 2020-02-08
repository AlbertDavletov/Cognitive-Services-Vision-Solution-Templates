using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Windows.UI.Xaml.Media;

namespace ShelfAuditingAutomation.Models
{
    public class ProductTag
    {
        public Tag Tag { get; set; }
        public ImageSource Image { get; set; }
        public ProductTag(Tag tag, string baseUrl)
        {
            this.Tag = tag;
            this.Image = Util.GetCanonicalImage(baseUrl, tag.Name);
        }
    }
}
