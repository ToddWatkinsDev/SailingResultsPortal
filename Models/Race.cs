using System.Collections.Generic;

namespace SailingResultsPortal.Models
{
    public class Race
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Handicap type: "Open" (open handicap) or "OneDesign"
        public string HandicapType { get; set; } = "Open";

        // Handicap system: "Portsmouth", "IRC", or "YTC"
        public string HandicapSystem { get; set; } = "Portsmouth";

        // Scoring system: "LowPoint" or "HighPoint"
        public string ScoringSystem { get; set; } = "LowPoint";

        // Whether this is a medal race (doubles points)
        public bool IsMedalRace { get; set; } = false;

        // Whether to include an overall class
        public bool IncludeOverall { get; set; } = true;

        public List<Class> Classes { get; set; } = new();
    }
}
