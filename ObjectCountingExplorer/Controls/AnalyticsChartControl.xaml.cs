using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using ObjectCountingExplorer.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ObjectCountingExplorer.Controls
{
    public sealed partial class AnalyticsChartControl : UserControl
    {
        private int MaxAxisValue = 0;

        public ObservableCollection<string> ChartLabelCollection { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<ChartItem> ChartItemCollection { get; set; } = new ObservableCollection<ChartItem>();

        public AnalyticsChartControl()
        {
            this.InitializeComponent();
        }

        public void UpdateChart(IEnumerable<ProductItemViewModel> productCollection)
        {
            Dictionary<string, List<PredictionModel>> productDict = productCollection.GroupBy(x => x.DisplayName).ToDictionary(x => x.Key, x => x.Select(y => y.Model).ToList());
            int maxProductCount = productDict.Any(x => x.Value.Any()) ? productDict.Max(x => x.Value.Count) : 0;
            int maxBarCharValue = maxProductCount > 6 ? maxProductCount : 6;
            SetAxisLabels(maxBarCharValue);

            ChartLabelCollection.Clear();
            ChartItemCollection.Clear();

            foreach (var item in productDict)
            {
                ChartLabelCollection.Add(item.Key);

                int highConfidenceCount = item.Value.Count(x => x.Probability > 0.6);
                int mediumConfidenceCount = item.Value.Count(x => x.Probability > 0.3 && x.Probability <= 0.6);
                int lowConfidenceCount = item.Value.Count(x => x.Probability <= 0.3);
                
                ChartItemCollection.Add(new ChartItem()
                {
                    Name = item.Key,
                    HighConfidenceCount = highConfidenceCount,
                    HighConfidenceWidth = (double)highConfidenceCount / MaxAxisValue,

                    MediumConfidenceCount = mediumConfidenceCount,
                    MediumConfidenceWidth = (double)mediumConfidenceCount / MaxAxisValue,

                    LowConfidenceCount = lowConfidenceCount,
                    LowConfidenceWidth = (double)lowConfidenceCount / MaxAxisValue,

                    EmptyWidth = 1 - (double)(highConfidenceCount + mediumConfidenceCount + lowConfidenceCount) / MaxAxisValue
                });
            }
        }

        private void OnChartGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void SetAxisLabels(int maxAxisValue, int minAxisValue = 0)
        {
            int axisLabelCount = 5;
            int axisDiffValue = GetDivider(maxAxisValue, axisLabelCount - 1);
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
        public int HighConfidenceCount { get; set; }
        public double HighConfidenceWidth { get; set; }

        public int MediumConfidenceCount { get; set; }
        public double MediumConfidenceWidth { get; set; }

        public int LowConfidenceCount { get; set; }
        public double LowConfidenceWidth { get; set; }

        public double EmptyWidth { get; set; }

        public static GridLength GetColumnWidth(double value)
        {
            return new GridLength(value, GridUnitType.Star);
        }
    }
}
