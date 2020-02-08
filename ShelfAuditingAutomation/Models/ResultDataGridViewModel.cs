using Windows.UI.Xaml.Media;

namespace ShelfAuditingAutomation.Models
{
    public class ResultDataGridViewModel
    {
        public string Name { get; set; }
        public int TotalCount { get; set; }
        public int ExpectedCount { get; set; }
        public double ExpectedCoverage { get; set; }
        public bool IsAggregateColumn { get; set; } = false;
        public bool IsColumnWithAlert { get; set; } = false;
        public ImageSource Image { get; set; }
    }
}
