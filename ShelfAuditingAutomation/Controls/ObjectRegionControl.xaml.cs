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
        LowConfidence,
        Edit,
        JustBorder,
        Collapsed
    }

    public sealed partial class ObjectRegionControl : UserControl, INotifyPropertyChanged
    {
        private static readonly double maxFontSize = 12;
        private static readonly double minFontSize = 2;
        private static readonly double maxTopMargin = 25;
        private static readonly double minTopMargin = 15;

        private RegionState prevState = RegionState.Active;
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

        public double zoomValue = 1;
        public double ZoomValue
        {
            get { return zoomValue; }
            set
            {
                zoomValue = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ZoomValue"));
                ZoomValueChanged();
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

                case RegionState.LowConfidence:
                    this.lowConfRegion.Stroke = highlightColor;
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

                case RegionState.LowConfidence:
                    this.lowConfRegion.Stroke = new SolidColorBrush(Color.FromArgb(255, 166, 216, 255));
                    break;
            }
        }

        private void OnMainGridTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (State != RegionState.Selected)
            {
                prevState = State;
            }

            switch (State)
            {
                case RegionState.Active:
                case RegionState.Selected:
                case RegionState.LowConfidence:

                    State = State == RegionState.Selected ? prevState : RegionState.Selected;

                    Canvas.SetZIndex(this, State == RegionState.Selected ? 10 : 1);
                    this.RegionSelected?.Invoke(this, new Tuple<RegionState, ProductItemViewModel>(State, ProductItemViewModel));
                    break;
            }
        }

        private void ZoomValueChanged()
        {
            double normalizedZoomValue = Util.NormalizeValue(ZoomValue, 1, 10);
            double fontSize = minFontSize + (1 - normalizedZoomValue) * (maxFontSize - minFontSize);
            double topMargin = minTopMargin + (1 - normalizedZoomValue) * (maxTopMargin - minTopMargin);

            this.selectedRegionTextBlock.FontSize = fontSize >= minFontSize && fontSize <= maxFontSize ? (int)fontSize : maxFontSize;
            this.selectedRegionCanvas.Margin = new Thickness(0, topMargin >= minTopMargin && topMargin <= maxTopMargin ? -topMargin : 0, 0, 0);
        }
    }
}
