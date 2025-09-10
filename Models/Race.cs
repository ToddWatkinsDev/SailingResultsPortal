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

        public List<Class> Classes { get; set; } = new();
    }
}
