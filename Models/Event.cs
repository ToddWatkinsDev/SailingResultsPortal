using System.Collections.Generic;

namespace SailingResultsPortal.Models
{
    public class Event
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<Race> Races { get; set; } = new();
    }

    public class Class
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string ScoringMethod { get; set; } = string.Empty;
        public double Rating { get; set; } = 1.0;
    }
}
