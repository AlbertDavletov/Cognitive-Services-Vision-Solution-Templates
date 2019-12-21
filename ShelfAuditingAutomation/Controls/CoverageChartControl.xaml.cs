using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace ShelfAuditingAutomation.Controls
{
    public sealed partial class CoverageChartControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(CoverageChartControl),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public CoverageChartControl()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        public void GenerateChart(List<ChartItem> chartItems, string subtitle = "")
        {
            this.subtitleTextBlock.Text = subtitle;

            this.chartGrid.Children.Clear();
            this.chartGrid.ColumnDefinitions.Clear();
            for (int index = 0; index < chartItems.Count; index++)
            {
                var item = chartItems[index];

                if (item.Value > 0)
                {
                    double percentage = Math.Round(item.Value * 100, 1);
                    ColumnDefinition column = new ColumnDefinition
                    {
                        Width = new GridLength(item.Value, GridUnitType.Star)
                    };
                    var border = new Border()
                    {
                        Background = item.Background,
                        Height = 24,
                        Child = new TextBlock()
                        {
                            Text = $"{percentage} %",
                            Foreground = item.Foreground,
                            TextWrapping = TextWrapping.NoWrap,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    };
                    ToolTip toolTip = new ToolTip
                    {
                        Content = percentage > 0 ? $"{item.Name}: {percentage} %" : $"{item.Name}: < 1 %"
                    };
                    ToolTipService.SetToolTip(border, toolTip);

                    this.chartGrid.ColumnDefinitions.Add(column);
                    this.chartGrid.Children.Add(border);
                    Grid.SetColumn(border, index);
                }
            }

            this.chartLegendItemsControl.ItemsSource = chartItems.Select(i => new Tuple<string, SolidColorBrush>(i.Name, i.Background));
        }
    }

    public class ChartItem
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public SolidColorBrush Background { get; set; }
        public SolidColorBrush Foreground { get; set; } = new SolidColorBrush(Colors.White);
    }
}
