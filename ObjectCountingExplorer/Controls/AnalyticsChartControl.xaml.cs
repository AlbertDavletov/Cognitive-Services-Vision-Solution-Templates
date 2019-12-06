using ObjectCountingExplorer.Models;
using ObjectCountingExplorer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace ObjectCountingExplorer.Controls
{
    public sealed partial class AnalyticsChartControl : UserControl
    {
        private int MaxAxisValue = 0;

        public ObservableCollection<string> ChartLabelCollection { get; set; } = new ObservableCollection<string>()
        {
            "High confidence",
            "Medium confidence",
            "Low confidence",
            "Unknown product",
            "Shelf gap"
        };

        public ObservableCollection<ChartItem> ChartItemCollection { get; set; } = new ObservableCollection<ChartItem>();

        public AnalyticsChartControl()
        {
            this.InitializeComponent();
        }

        public void UpdateChart(IEnumerable<ProductItemViewModel> productCollection)
        {
            var emptyGapList =       productCollection.Where(p => p.DisplayName.Equals(Util.EmptyGapName, StringComparison.OrdinalIgnoreCase)).Select(p => p.Id).ToList();
            var unknownProductList = productCollection.Where(p => p.DisplayName.Equals(Util.UnknownProductName, StringComparison.OrdinalIgnoreCase)).Select(p => p.Id).ToList();
            
            var highConfidenceList =   productCollection.Where(p => p.Model.Probability >= Util.MinHighProbability && !emptyGapList.Contains(p.Id) && !unknownProductList.Contains(p.Id)).ToList();
            var mediumConfidenceList = productCollection.Where(p => p.Model.Probability >= Util.MinMediumProbability && p.Model.Probability < Util.MinHighProbability && !emptyGapList.Contains(p.Id) && !unknownProductList.Contains(p.Id)).ToList();
            var lowConfidenceList =    productCollection.Where(p => p.Model.Probability < Util.MinMediumProbability && !emptyGapList.Contains(p.Id) && !unknownProductList.Contains(p.Id)).ToList();

            int maxProductCount = (int)Util.Max(emptyGapList.Count, unknownProductList.Count, highConfidenceList.Count, mediumConfidenceList.Count, lowConfidenceList.Count);
            int maxBarCharValue = maxProductCount > 6 ? maxProductCount : 6;
            SetAxisLabels(maxBarCharValue);


            ChartItemCollection.Clear();
            ChartItemCollection.Add(new ChartItem()
            {
                Name = "High confidence",
                ItemCount = highConfidenceList.Count,
                ItemWidth = (double)highConfidenceList.Count / MaxAxisValue,
                EmptyWidth = 1 - (double)highConfidenceList.Count / MaxAxisValue,
                Color = new SolidColorBrush(Util.HighConfidenceColor)
            });
            ChartItemCollection.Add(new ChartItem()
            {
                Name = "Medium confidence",
                ItemCount = mediumConfidenceList.Count,
                ItemWidth = (double)mediumConfidenceList.Count / MaxAxisValue,
                EmptyWidth = 1 - (double)mediumConfidenceList.Count / MaxAxisValue,
                Color = new SolidColorBrush(Util.MediumConfidenceColor)
            });
            ChartItemCollection.Add(new ChartItem()
            {
                Name = "Low confidence",
                ItemCount = lowConfidenceList.Count,
                ItemWidth = (double)lowConfidenceList.Count / MaxAxisValue,
                EmptyWidth = 1 - (double)lowConfidenceList.Count / MaxAxisValue,
                Color = new SolidColorBrush(Util.LowConfidenceColor)
            });
            ChartItemCollection.Add(new ChartItem()
            {
                Name = "Unknown product",
                ItemCount = unknownProductList.Count,
                ItemWidth = (double)unknownProductList.Count / MaxAxisValue,
                EmptyWidth = 1 - (double)unknownProductList.Count / MaxAxisValue,
                Color = new SolidColorBrush(Util.UnknownProductColor)
            });
            ChartItemCollection.Add(new ChartItem()
            {
                Name = "Shelf gap",
                ItemCount = emptyGapList.Count,
                ItemWidth = (double)emptyGapList.Count / MaxAxisValue,
                EmptyWidth = 1 - (double)emptyGapList.Count / MaxAxisValue,
                Color = new SolidColorBrush(Util.EmptyGapColor)
            });
        }

        private void SetAxisLabels(int maxAxisValue, int minAxisValue = 0)
        {
            int axisLabelCount = 5;
            int axisDiffValue = (int)Math.Ceiling((double)maxAxisValue / (axisLabelCount - 1));
            this.x1_Lbl.Text = $"{minAxisValue + 0 * axisDiffValue}";
            this.x2_Lbl.Text = $"{minAxisValue + 1 * axisDiffValue}";
            this.x3_Lbl.Text = $"{minAxisValue + 2 * axisDiffValue}";
            this.x4_Lbl.Text = $"{minAxisValue + 3 * axisDiffValue}";
            this.x5_Lbl.Text = $"{minAxisValue + 4 * axisDiffValue}";
            MaxAxisValue = minAxisValue + 4 * axisDiffValue;
        }

        private int GetDivider(int number, int divisor)
        {
            int div = number / divisor;
            int remainder = number % div;
            return remainder == 0 ? div : div + remainder;
        }
    }

    public class ChartItem
    {
        public string Name { get; set; }
        public int ItemCount { get; set; }
        public double ItemWidth { get; set; }
        public double EmptyWidth { get; set; }
        public SolidColorBrush Color { get; set; }

        public static GridLength GetColumnWidth(double value)
        {
            return new GridLength(value, GridUnitType.Star);
        }
    }
}
