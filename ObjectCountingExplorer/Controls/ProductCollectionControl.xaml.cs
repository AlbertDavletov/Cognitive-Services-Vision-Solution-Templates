using ObjectCountingExplorer.Models;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ObjectCountingExplorer.Controls
{
    public sealed partial class ProductCollectionControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(ProductCollectionControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ProductCollectionProperty =
            DependencyProperty.Register(
                "ProductCollection",
                typeof(ObservableCollection<ProductItemViewModel>),
                typeof(ProductCollectionControl),
                new PropertyMetadata(null));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public ObservableCollection<ProductItemViewModel> ProductCollection
        {
            get { return (ObservableCollection<ProductItemViewModel>)GetValue(ProductCollectionProperty); }
            set { SetValue(ProductCollectionProperty, value); }
        }

        public ProductCollectionControl()
        {
            this.InitializeComponent();
        }
    }
}
