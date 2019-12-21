using System;

namespace ShelfAuditingAutomation.Models
{
    public enum AppViewState
    {
        ImageSelection,
        ImageSelected,
        ImageAddOrUpdateProduct,
        ImageAnalysisReview,
        ImageAnalysisPublishing
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
