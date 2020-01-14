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
        public AreaCoverage ExpectedAreaCoverage { get; set; }
        public double LowConfidence { get; set; }
        public List<SpecItem> Items { get; set; }
    }

    public class SpecItem
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; }
        public int ExpectedCount { get; set; }
        public double ExpectedAreaCoverage { get; set; }
    }

    public class AreaCoverage
    {
        public double Tagged { get; set; }
        public double Unknown { get; set; }
        public double Gap { get; set; }
    }
}
