using ShelfAuditingAutomation.Models;
using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace ShelfAuditingAutomation.Controls
{
    public enum RegionState
    {
        Disabled,
        Active,
        Selected,
        Edit,
        JustBorder,
        Collapsed
    }

    public sealed partial class ObjectRegionControl : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<Tuple<RegionState, ProductItemViewModel>> RegionSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public ProductItemViewModel ProductItemViewModel { get; set; }

        private string title = string.Empty;
        public string Title
        {
            get { return this.title; }
            set
            {
                this.title = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
            }
        }

        private RegionState state = RegionState.Active;
        public RegionState State
        {
            get { return this.state; }
            set
            {
                this.state = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("State"));
            }
        }

        private Color color = Colors.Lime;
        public Color Color
        {
            get { return this.color; }
            set
            {
                this.color = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
            }
        }

        public ObjectRegionControl()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void MainGridPointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var highlightColor = new SolidColorBrush(Colors.White);
            var borderSize = new Thickness(1);
            switch (State)
            {
                case RegionState.Active:
                    this.activeRegion.BorderBrush = highlightColor;
                    break;

                case RegionState.Selected:
                    this.labelPanel.BorderThickness = borderSize;
                    this.labelPanel.BorderBrush = highlightColor;
                    this.selectedRegion.BorderBrush = highlightColor;
                    break;
            }
        }

        private void MainGridPointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var borderSize = new Thickness(0);
            switch (State)
            {
                case RegionState.Active:
                    this.activeRegion.BorderBrush = new SolidColorBrush(Color);
                    break;

                case RegionState.Disabled:
                    this.disabledRegion.BorderThickness = borderSize;
                    break;

                case RegionState.Selected:
                    this.labelPanel.BorderThickness = borderSize;
                    this.selectedRegion.BorderBrush = new SolidColorBrush(Color);
                    break;
            }
        }

        private void OnMainGridTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (State == RegionState.Active || State == RegionState.Selected)
            {
                State = State == RegionState.Selected ? RegionState.Active : RegionState.Selected;
                this.RegionSelected?.Invoke(this, new Tuple<RegionState, ProductItemViewModel>(State, ProductItemViewModel));
            }
        }
    }
}
