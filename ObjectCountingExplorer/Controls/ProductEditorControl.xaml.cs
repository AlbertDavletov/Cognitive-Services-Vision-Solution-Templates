using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
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
    public sealed partial class ProductEditorControl : UserControl, INotifyPropertyChanged
    {
        private bool isQuickAccess = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ProjectTagCollectionProperty =
            DependencyProperty.Register(
                "ProjectTagCollection",
                typeof(ObservableCollection<ProductTag>),
                typeof(ProductEditorControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty RecentlyUsedTagCollectionProperty =
            DependencyProperty.Register(
                "RecentlyUsedTagCollection",
                typeof(ObservableCollection<ProductTag>),
                typeof(ProductEditorControl),
                new PropertyMetadata(null));

        public ObservableCollection<ProductTag> ProjectTagCollection
        {
            get { return (ObservableCollection<ProductTag>)GetValue(ProjectTagCollectionProperty); }
            set { SetValue(ProjectTagCollectionProperty, value); }
        }

        public ObservableCollection<ProductTag> RecentlyUsedTagCollection
        {
            get { return (ObservableCollection<ProductTag>)GetValue(RecentlyUsedTagCollectionProperty); }
            set { SetValue(RecentlyUsedTagCollectionProperty, value); }
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

        private ProductTag currentTag;
        public ProductTag CurrentTag
        {
            get { return currentTag; }
            set
            {
                currentTag = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentTag"));
                CurrentProductChanged();
            }
        }

        public event EventHandler EditorClosed;
        public event EventHandler<Tuple<UpdateMode, ProductTag>> ProductTagUpdated; 

        public ProductEditorControl()
        {
            this.InitializeComponent();
        }

        private void CurrentProductChanged()
        {
            string tagId = CurrentTag?.Tag?.Id.ToString();
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
            this.productGridView.ItemsSource = string.IsNullOrEmpty(filter) ? ProjectTagCollection : ProjectTagCollection.Where(x => x.Tag.Name.ToLower().Contains(filter));
        }

        private void OnProductClick(object sender, ItemClickEventArgs e)
        {
            string tag = ((GridView)sender).Tag?.ToString() ?? string.Empty;
            if (e.ClickedItem is ProductTag tagEntry)
            {
                isQuickAccess = tag.Equals("QuickAccess", StringComparison.OrdinalIgnoreCase);
                CurrentTag = tagEntry;
                this.ProductTagUpdated?.Invoke(this, new Tuple<UpdateMode, ProductTag>(
                    EditorState == EditorState.Add ? UpdateMode.UpdateNewProduct : UpdateMode.UpdateExistingProduct,
                    CurrentTag));
            }
        }

        private void OnEditorUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            if (CurrentTag != null)
            {
                this.ProductTagUpdated?.Invoke(this, new Tuple<UpdateMode, ProductTag>(
                    EditorState == EditorState.Add ? UpdateMode.SaveNewProduct : UpdateMode.SaveExistingProduct,
                    CurrentTag));
            }
        }

        private void UpdateRecentlyUsedProducts()
        {
            RecentlyUsedTagCollection.Clear();
            foreach (var tagId in SettingsHelper.Instance.RecentlyUsedProducts)
            {
                var productTag = ProjectTagCollection.FirstOrDefault(p => p.Tag.Id.ToString() == tagId);
                if (productTag != null)
                {
                    RecentlyUsedTagCollection.Add(productTag);
                }
            }
        }
    }
}
