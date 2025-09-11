using System;
using System.ComponentModel.DataAnnotations;

namespace SailingResultsPortal.Models
{
    public class BulkResultDto
    {
        public string RaceId { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sailor name is required")]
        [StringLength(100, ErrorMessage = "Sailor name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-'\.]+$", ErrorMessage = "Sailor name contains invalid characters")]
        public string SailorName { get; set; } = string.Empty;

        public string SailNumber { get; set; } = string.Empty;
        public string? FinishTime { get; set; } // String format for easier JSON input
        public string? Status { get; set; }
        public double? HandicapNumber { get; set; } // For Open handicap races

        // Convert to Result model
        public Result ToResult()
        {
            var result = new Result
            {
                RaceId = this.RaceId,
                ClassId = this.ClassId,
                SailorName = this.SailorName,
                SailNumber = this.SailNumber,
                Status = this.Status
            };

            // Parse finish time if provided
            if (!string.IsNullOrWhiteSpace(this.FinishTime))
            {
                if (TimeSpan.TryParse(this.FinishTime, out var finishTime))
                {
                    result.FinishTime = finishTime;
                }
            }

            // Set handicap number if provided
            result.HandicapNumber = this.HandicapNumber;

            return result;
        }
    }
}