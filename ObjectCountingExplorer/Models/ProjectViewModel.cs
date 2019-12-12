using System;

namespace ObjectCountingExplorer.Models
{
    public enum AppViewState
    {
        ImageSelection,
        ImageSelected,
        ImageAnalyzed,
        ImageAddOrUpdateProduct,
        ImageAnalysisReview,
        ImageAnalysisPublish,
        ImageAnalysisPublishing,
    }

    public enum SummaryViewState
    {
        GroupedByCategory,
        GroupedByTag
    }

    public class ProjectViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ProjectViewModel(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
