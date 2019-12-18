using System;
using System.Collections.Generic;

namespace ShelfAuditingAutomation.Models
{
    public class SpecsData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid ModelId { get; set; }
        public string ModelName { get; set; }
        public string[] SampleImages { get; set; }
        public List<SpecItem> Items { get; set; }
    }

    public class SpecItem
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; }
        public double ExpectedAreaCoverage { get; set; }
    }
}
