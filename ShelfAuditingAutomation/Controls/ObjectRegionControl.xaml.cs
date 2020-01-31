using ShelfAuditingAutomation.Models;
using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace ShelfAuditingAutomation.Controls
{
    public enum RegionState
    {
        Active,
        Edit,
        Disabled,
        Selected,
        SelectedWithNotification,
        LowConfidence
    }

    public enum ActionType
    {
        Apply,
        Edit,
        Cancel
    }

    public sealed partial class ObjectRegionControl : UserControl, INotifyPropertyChanged
    {
        private static readonly double MinScale = 0.15;

        public event EventHandler RegionSelected;
        public event EventHandler<ActionType> RegionActionSelected;

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
                UpdateZIndex();
            }
        }

        private bool enableSelectedIcon = false;
        public bool EnableSelectedIcon
        {
            get { return this.enableSelectedIcon; }
            set
            {
                this.enableSelectedIcon = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EnableSelectedIcon"));
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
            this.RegionSelected?.Invoke(this, EventArgs.Empty);
        }

        private void CanvasNotificationGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.canvasNotification.Visibility = Visibility.Visible;

            UpdateCanvasNotificationSize();
        }

        private void OnNotificationApplyButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.applyLabelButton.IsChecked.GetValueOrDefault())
            {
                this.RegionActionSelected?.Invoke(this, ActionType.Apply);
            }
            else if (this.editLabelButton.IsChecked.GetValueOrDefault())
            {
                this.RegionActionSelected?.Invoke(this, ActionType.Edit);
            }
            else
            {
                this.RegionActionSelected?.Invoke(this, ActionType.Cancel);
            }
        }

        private void OnNotificationCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.RegionActionSelected?.Invoke(this, ActionType.Cancel);
        }

        private void OnToggleOptionButtonClicked(object sender, RoutedEventArgs e)
        {
            string tag = ((ToggleButton)sender)?.Tag?.ToString();
            bool isApplyAction = string.Equals(tag, "Apply", StringComparison.OrdinalIgnoreCase);

            this.applyLabelButton.IsChecked = isApplyAction;
            this.editLabelButton.IsChecked = !isApplyAction;
        }

        private void UpdateZIndex()
        {
            int zIndex = 1;
            switch (State)
            {
                case RegionState.Edit:
                    zIndex = 20;
                    break;

                case RegionState.Selected:
                case RegionState.SelectedWithNotification:
                    zIndex = 15;
                    break;

                case RegionState.Active:
                case RegionState.LowConfidence:
                    zIndex = 10;
                    break;

                case RegionState.Disabled:
                    zIndex = 1;
                    break;
            }
            Canvas.SetZIndex(this, zIndex);
        }

        private void ZoomValueChanged()
        {
            double normalizedZoomValue = Util.NormalizeValue(ZoomValue, 1, 10);

            double activeEllipseSize = Util.GetScaledValue(normalizedZoomValue, 4, 12);
            this.activeRegionEllipse.Width = activeEllipseSize;
            this.activeRegionEllipse.Height = activeEllipseSize;

            double lowConfEllipseSize = Util.GetScaledValue(normalizedZoomValue, 8, 24);
            this.lowConfRegionBorder.Width = lowConfEllipseSize;
            this.lowConfRegionBorder.Height = lowConfEllipseSize;
            this.lowConfRegionIcon.FontSize = Util.GetScaledValue(normalizedZoomValue, 6, 14);

            double disableEllipseSize = Util.GetScaledValue(normalizedZoomValue, 2, 8);
            this.disabledRegionEllipse.Width = disableEllipseSize;
            this.disabledRegionEllipse.Height = disableEllipseSize;

            double selectedItemFontSize = Util.GetScaledValue(normalizedZoomValue, 2, 12);
            this.selectedRegionTextBlock.FontSize = selectedItemFontSize;
            this.selectedRegionIcon.FontSize = selectedItemFontSize;
            this.selectedRegionCanvas.Margin = new Thickness(0, -Util.GetScaledValue(normalizedZoomValue, 15, 25), 0, 0);

            UpdateCanvasNotificationSize();
        }

        private void UpdateCanvasNotificationSize()
        {
            double scale;
            if (ZoomValue == 1)
            {
                scale = 1;
            }
            else
            {
                double newScale = 0.7 - 0.1 * (ZoomValue - 2);
                scale = newScale >= MinScale ? newScale : MinScale;
            }

            this.canvasNotificationGridScale.ScaleX = scale; 
            this.canvasNotificationGridScale.ScaleY = scale;

            var parent = (FrameworkElement)this.Parent;
            double topOffset = 4;
            double gridWidth = this.canvasNotificationGrid.ActualWidth * scale;
            double gridHeight = this.canvasNotificationGrid.ActualHeight * scale;

            if (parent != null && gridWidth > 0 && gridHeight > 0)
            {
                this.canvasNotification.Margin = new Thickness(
                    this.Margin.Left + gridWidth > parent.ActualWidth ? -(this.Margin.Left + gridWidth - parent.ActualWidth) : 0,
                    this.Margin.Top < (gridHeight + topOffset) ? this.ActualHeight + topOffset : -(gridHeight + topOffset),
                    0, 0);
            }
        }
    }
}
