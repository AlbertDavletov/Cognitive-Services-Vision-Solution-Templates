﻿using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace ShelfAuditingAutomation.Controls
{
    public class RegionEditorViewModel : INotifyPropertyChanged
    {
        private static readonly double maxFontSize = 12;
        private static readonly double minFontSize = 2;
        private static readonly double maxTopMargin = 30;
        private static readonly double minTopMargin = 17;

        public event PropertyChangedEventHandler PropertyChanged;

        public Guid ProductId { get; set; }
        public string Title { get; set; }
        public PredictionModel Model { get; set; }

        public bool EnableRemove { get; set; } = false;

        private Color color = Colors.White;
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

        public double labelFontSize = maxFontSize;
        public double LabelFontSize
        {
            get { return labelFontSize; }
            set
            {
                labelFontSize = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LabelFontSize"));
            }
        }

        public Thickness labelMargin = new Thickness(0, -30, 0, 0);
        public Thickness LabelMargin
        {
            get { return labelMargin; }
            set
            {
                labelMargin = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LabelMargin"));
            }
        }

        private void ZoomValueChanged()
        {
            double normalizedZoomValue = Util.NormalizeValue(ZoomValue, 1, 10);

            LabelFontSize = Util.GetScaledValue(normalizedZoomValue, minFontSize, maxFontSize);
            if (!EnableRemove)
            {
                LabelMargin = new Thickness(0, -Util.GetScaledValue(normalizedZoomValue, minTopMargin, maxTopMargin), 0, 0);
            }
        }
    }

    public sealed partial class RegionEditorControl : UserControl
    {
        public event EventHandler<Guid> RegionChanged;
        public event EventHandler<Guid> RegionDeleted;

        public RegionEditorControl()
        {
            this.InitializeComponent();
        }

        private void OnTopLeftManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (((Thumb)sender).DataContext is RegionEditorViewModel dataContext)
            {
                double zoom = dataContext.ZoomValue > 0 ? dataContext.ZoomValue : 1.0;
                AdjustBoundingBoxSize(e.Delta.Translation.X / zoom, e.Delta.Translation.Y / zoom, 0, 0);
            }
        }

        private void OnBottomRightManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (((Thumb)sender).DataContext is RegionEditorViewModel dataContext)
            {
                double zoom = dataContext.ZoomValue > 0 ? dataContext.ZoomValue : 1.0;
                AdjustBoundingBoxSize(0, 0, e.Delta.Translation.X / zoom, e.Delta.Translation.Y / zoom);
            }
        }

        private void OnTopRightManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (((Thumb)sender).DataContext is RegionEditorViewModel dataContext)
            {
                double zoom = dataContext.ZoomValue > 0 ? dataContext.ZoomValue : 1.0;
                AdjustBoundingBoxSize(0, e.Delta.Translation.Y / zoom, e.Delta.Translation.X / zoom, 0);
            }
        }

        private void OnBottomLeftManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (((Thumb)sender).DataContext is RegionEditorViewModel dataContext)
            {
                double zoom = dataContext.ZoomValue > 0 ? dataContext.ZoomValue : 1.0;
                AdjustBoundingBoxSize(e.Delta.Translation.X / zoom, 0, 0, e.Delta.Translation.Y / zoom);
            }
        }

        private void AdjustBoundingBoxSize(double leftOffset, double topOffset, double widthOffset, double heightOffset)
        {
            var parentWidth = (this.Parent as FrameworkElement)?.ActualWidth ?? double.MaxValue;
            var parentHeight = (this.Parent as FrameworkElement)?.ActualHeight ?? double.MaxValue;

            double newWidth = this.Width + widthOffset - leftOffset;
            double newHeight = this.Height + heightOffset - topOffset;

            double newLeftOffset = (this.Margin.Left + leftOffset) >= 0 ? this.Margin.Left + leftOffset : 0;
            double newTopOffset = (this.Margin.Top + topOffset) >= 0 ? this.Margin.Top + topOffset : 0;

            bool isValidXOffset = (newLeftOffset + newWidth) <= parentWidth;
            bool isValidYOffset = (newTopOffset + newHeight) <= parentHeight;

            if (isValidXOffset && isValidYOffset && newWidth >= 5 && newHeight >= 5)
            {
                this.Width = newWidth;
                this.Height = newHeight;

                this.Margin = new Thickness(
                    newLeftOffset,
                    newTopOffset,
                    this.Margin.Right,
                    this.Margin.Bottom);
            }
        }

        private void OnThumbReleased(object sender, PointerRoutedEventArgs e)
        {
            var dataContext = ((Thumb)sender).DataContext as RegionEditorViewModel;
            this.RegionChanged?.Invoke(this, dataContext.ProductId);
        }

        private void OnRegionRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            var dataContext = ((Button)sender).DataContext as RegionEditorViewModel;
            this.RegionDeleted?.Invoke(this, dataContext.ProductId);
        }
    }
}