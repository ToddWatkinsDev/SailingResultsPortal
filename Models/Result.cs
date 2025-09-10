using System;

namespace SailingResultsPortal.Models
{
    public class Result
    {
        public string Id { get; set; } = string.Empty;
        public string RaceId { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string SailorName { get; set; } = string.Empty;
        public string SailNumber { get; set; } = string.Empty;  // Added SailNumber for completeness
        public TimeSpan FinishTime { get; set; }
        public double CorrectedTime { get; set; } // Handicap corrected (calculated)
        public int Position { get; set; } // Finish position
    }
}
