using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace ObjectCountingExplorer.Controls
{
    public class RegionEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<Tag> AvailableTags { get; set; }
        public PredictionModel Region { get; set; }

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
    }

    public sealed partial class RegionEditorControl : UserControl   
    {
        public event EventHandler<Tag> RegionChanged;
        public event EventHandler RegionDeleted;

        public RegionEditorControl()
        {
            this.InitializeComponent();
        }

        private void OnTopLeftDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(e.HorizontalChange, e.VerticalChange, 0, 0);
        }

        private void OnBottomRightDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(0, 0, e.HorizontalChange, e.VerticalChange);
        }

        private void OnTopRightDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(0, e.VerticalChange, e.HorizontalChange, 0);
        }

        private void OnBottomLeftDragDelta(object sender, DragDeltaEventArgs e)
        {
            AdjustBoundingBoxSize(e.HorizontalChange, 0, 0, e.VerticalChange);
        }

        private void AdjustBoundingBoxSize(double leftOffset, double topOffset, double widthOffset, double heightOffset)
        {
            double newWidth = this.ActualWidth + widthOffset - leftOffset;
            double newHeight = this.ActualHeight + heightOffset - topOffset;

            if ((newWidth >= 0) && (newHeight >= 0))
            {
                this.Width = newWidth;
                this.Height = newHeight;

                this.Margin = new Thickness(
                    this.Margin.Left + leftOffset,
                    this.Margin.Top + topOffset,
                    this.Margin.Right,
                    this.Margin.Bottom);
            }
        }

        private void OnThumbReleased(object sender, PointerRoutedEventArgs e)
        {
            var selectedTag = this.tagComboBox.SelectedItem as Tag;
            this.RegionChanged?.Invoke(this, selectedTag);
        }

        private void DeleteRegionClicked(object sender, RoutedEventArgs e)
        {
            this.RegionDeleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnTagComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Any())
            {
                var selectedTag = this.tagComboBox.SelectedItem as Tag;
                this.RegionChanged?.Invoke(this, selectedTag);
            }
        }
    }
}
