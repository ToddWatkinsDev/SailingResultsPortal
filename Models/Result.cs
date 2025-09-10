using System;

namespace SailingResultsPortal.Models
{
    public class Result
    {
        public string Id { get; set; }
        public string RaceId { get; set; }
        public string ClassId { get; set; }
        public string SailorName { get; set; }
        public TimeSpan FinishTime { get; set; } // Raw finish time
        public double CorrectedTime { get; set; } // Handicap corrected (calculated)
        public int Position { get; set; } // Finish position
    }
}
