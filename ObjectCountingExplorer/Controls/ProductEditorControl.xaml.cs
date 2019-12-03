using ObjectCountingExplorer.Helpers;
using ObjectCountingExplorer.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ObjectCountingExplorer.Controls
{
    public enum EditorState
    {
        Add,
        Edit
    }

    public enum UpdateMode
    {
        UpdateNewProduct,
        UpdateExistingProduct,
        SaveNewProduct,
        SaveExistingProduct
    }

    public sealed partial class ProductEditorControl : UserControl, INotifyPropertyChanged
    {
        private bool isQuickAccess = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ProductCollectionProperty =
            DependencyProperty.Register(
                "ProductCollection",
                typeof(ObservableCollection<ProductItemViewModel>),
                typeof(ProductEditorControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty RecentlyUsedProductCollectionProperty =
            DependencyProperty.Register(
                "RecentlyUsedProductCollection",
                typeof(ObservableCollection<ProductItemViewModel>),
                typeof(ProductEditorControl),
                new PropertyMetadata(null));

        public ObservableCollection<ProductItemViewModel> ProductCollection
        {
            get { return (ObservableCollection<ProductItemViewModel>)GetValue(ProductCollectionProperty); }
            set { SetValue(ProductCollectionProperty, value); }
        }

        public ObservableCollection<ProductItemViewModel> RecentlyUsedProductCollection
        {
            get { return (ObservableCollection<ProductItemViewModel>)GetValue(RecentlyUsedProductCollectionProperty); }
            set { SetValue(RecentlyUsedProductCollectionProperty, value); }
        }

        private EditorState editorState;
        public EditorState EditorState
        {
            get { return editorState; }
            set
            {
                editorState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EditorState"));
            }
        }

        private ProductItemViewModel currentProduct;
        public ProductItemViewModel CurrentProduct
        {
            get { return currentProduct; }
            set
            {
                currentProduct = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProduct"));
                CurrentProductChanged();
            }
        }

        public event EventHandler EditorClosed;
        public event EventHandler<Tuple<UpdateMode, ProductItemViewModel>> ProductUpdated; 

        public ProductEditorControl()
        {
            this.InitializeComponent();
        }

        private void CurrentProductChanged()
        {
            string tagId = CurrentProduct?.Model?.TagId.ToString();
            if (tagId != null && !isQuickAccess)
            {
                var recentProductList = SettingsHelper.Instance.RecentlyUsedProducts;
                recentProductList.Insert(0, tagId);
                recentProductList = recentProductList.GroupBy(p => p).Select(p => p.Key).Take(5).ToList();
                SettingsHelper.Instance.RecentlyUsedProducts = recentProductList;

                UpdateRecentlyUsedProducts();
            }
        }

        private void OnCancelEditorButtonClick(object sender, RoutedEventArgs e)
        {
            this.EditorClosed?.Invoke(this, EventArgs.Empty);
        }

        private void OnFindProductTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            string filter = (this.findProductTextBox?.Text ?? string.Empty).ToLower();
            this.productGridView.ItemsSource = string.IsNullOrEmpty(filter) ? ProductCollection : ProductCollection.Where(x => x.DisplayName.ToLower().Contains(filter));
        }

        private void OnProductClick(object sender, ItemClickEventArgs e)
        {
            string tag = ((GridView)sender).Tag?.ToString() ?? string.Empty;
            if (e.ClickedItem is ProductItemViewModel productEntry)
            {
                isQuickAccess = tag.Equals("QuickAccess", StringComparison.OrdinalIgnoreCase);
                CurrentProduct = productEntry;
                this.ProductUpdated?.Invoke(this, new Tuple<UpdateMode, ProductItemViewModel>(
                    EditorState == EditorState.Add ? UpdateMode.UpdateNewProduct : UpdateMode.UpdateExistingProduct, 
                    CurrentProduct));
            }
        }

        private void OnEditorUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            if (CurrentProduct != null)
            {
                this.ProductUpdated?.Invoke(this, new Tuple<UpdateMode, ProductItemViewModel>(
                    EditorState == EditorState.Add ? UpdateMode.SaveNewProduct : UpdateMode.SaveExistingProduct,
                    CurrentProduct));
            }
        }

        private void UpdateRecentlyUsedProducts()
        {
            RecentlyUsedProductCollection.Clear();
            foreach (var tagId in SettingsHelper.Instance.RecentlyUsedProducts)
            {
                var product = ProductCollection.FirstOrDefault(p => p.Model.TagId.ToString() == tagId);
                if (product != null)
                {
                    RecentlyUsedProductCollection.Add(product);
                }
            }
        }
    }
}
