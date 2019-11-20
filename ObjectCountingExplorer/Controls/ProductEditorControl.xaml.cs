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
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ProductCollectionProperty =
            DependencyProperty.Register(
                "ProductCollection",
                typeof(ObservableCollection<ProductItemViewModel>),
                typeof(ProductEditorControl),
                new PropertyMetadata(null));

        public ObservableCollection<ProductItemViewModel> ProductCollection
        {
            get { return (ObservableCollection<ProductItemViewModel>)GetValue(ProductCollectionProperty); }
            set { SetValue(ProductCollectionProperty, value); }
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
            }
        }

        public event EventHandler EditorClosed;
        public event EventHandler<Tuple<UpdateMode, ProductItemViewModel>> ProductUpdated; 

        public ProductEditorControl()
        {
            this.InitializeComponent();
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
            if (e.ClickedItem is ProductItemViewModel productEntry)
            {
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
    }
}
