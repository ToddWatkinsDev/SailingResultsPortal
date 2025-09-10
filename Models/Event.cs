using System;
using System.Collections.Generic;

namespace SailingResultsPortal.Models
{
    public class Event
    {
        public string Id { get; set; } // Unique identifier
        public string Name { get; set; }
        public List<Race> Races { get; set; } = new();
    }

    public class Race
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Class> Classes { get; set; } = new();
    }

    public class Class
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ScoringMethod { get; set; } // e.g. "Portsmouth", "IRC", "YTC"
    }
}
