using ShelfAuditingAutomation.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ShelfAuditingAutomation.Views
{
    public sealed partial class ReviewPanelControl : UserControl
    {
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register(
                "State",
                typeof(AppViewState),
                typeof(ReviewPanelControl),
                new PropertyMetadata(AppViewState.ImageAnalysisReview, OnStateChanged));

        public static readonly DependencyProperty ResultDataGridCollectionProperty =
            DependencyProperty.Register(
                "ResultDataGridCollection",
                typeof(ObservableCollection<ResultDataGridViewModel>),
                typeof(ReviewPanelControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty GroupedProductCollectionProperty =
            DependencyProperty.Register(
                "GroupedProductCollection",
                typeof(ObservableCollection<Tuple<string, List<ProductItemViewModel>>>),
                typeof(ReviewPanelControl),
                new PropertyMetadata(null));

        public ObservableCollection<ResultDataGridViewModel> ResultDataGridCollection
        {
            get { return (ObservableCollection<ResultDataGridViewModel>)GetValue(ResultDataGridCollectionProperty); }
            set { SetValue(ResultDataGridCollectionProperty, value); }
        }

        public ObservableCollection<Tuple<string, List<ProductItemViewModel>>> GroupedProductCollection
        {
            get { return (ObservableCollection<Tuple<string, List<ProductItemViewModel>>>)GetValue(GroupedProductCollectionProperty); }
            set { SetValue(GroupedProductCollectionProperty, value); }
        }

        public AppViewState State
        {
            get { return (AppViewState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public event EventHandler Closed;
        public event EventHandler PublishResults;
        public event EventHandler<IEnumerable<ResultDataGridViewModel>> DataGridSelected;
        public event EventHandler<ProductItemViewModel> ProductSelected;

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ReviewPanelControl)d).OnStateChanged();
        }

        public ReviewPanelControl()
        {
            this.InitializeComponent();
        }

        private void OnStateChanged()
        {
            this.yesRadioButton.IsChecked = null;
            this.noRadioButton.IsChecked = null;
            this.submitButon.IsEnabled = false;

            this.pivot.SelectedIndex = 0;
            this.dataGrid.Visibility = Visibility.Visible;
            this.groupedProductListView.Visibility = Visibility.Collapsed;
        }

        private void RadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                string tag = rb.Tag.ToString();
                switch (tag)
                {
                    case "Yes":
                        this.submitButon.Content = "Publish results";
                        break;

                    case "No":
                        this.submitButon.Content = "Continue";
                        break;
                }
                this.submitButon.IsEnabled = true;
            }
        }

        private void OnSubmitButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.yesRadioButton.IsChecked.GetValueOrDefault())
            {
                this.PublishResults?.Invoke(this, EventArgs.Empty);
            }
            else if (this.noRadioButton.IsChecked.GetValueOrDefault())
            {
                this.Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnPublishButtonClicked(object sender, RoutedEventArgs e)
        {
            this.PublishResults?.Invoke(this, EventArgs.Empty);
        }

        private void OnClosePanelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.Closed?.Invoke(this, EventArgs.Empty);
        }

        private void PivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.pivot.SelectedIndex)
            {
                case 0:
                    this.dataGrid.Visibility = Visibility.Visible;
                    this.groupedProductListView.Visibility = Visibility.Collapsed;

                    ApplyFilters();
                    break;

                case 1:
                    this.dataGrid.Visibility = Visibility.Collapsed;
                    this.groupedProductListView.Visibility = Visibility.Visible;

                    ApplyFilters(useFilters: false);
                    break;
            }
        }

        private void ProductCollectionControlProductSelected(object sender, ProductItemViewModel e)
        {
            this.ProductSelected?.Invoke(this, e);
        }

        private void FilterChecked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterUnchecked(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters(bool useFilters = true)
        {
            this.DataGridSelected?.Invoke(this, useFilters ? ResultDataGridCollection.Where(f => f.IsChecked) : ResultDataGridCollection);
        }
    }
}
