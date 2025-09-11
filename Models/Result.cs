using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailingResultsPortal.Models
{
    // JSON wrapper with timestamp for caching
    public class JsonDataWrapper<T>
    {
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<T> Data { get; set; } = new();
    }

    public class Result
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Race ID is required")]
        public string RaceId { get; set; } = string.Empty;

        public string ClassId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sailor name is required")]
        [StringLength(100, ErrorMessage = "Sailor name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-'\.]+$", ErrorMessage = "Sailor name contains invalid characters")]
        public string SailorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sail number is required")]
        [StringLength(20, ErrorMessage = "Sail number cannot exceed 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\-/\s]+$", ErrorMessage = "Sail number contains invalid characters")]
        public string SailNumber { get; set; } = string.Empty;

        public TimeSpan? FinishTime { get; set; } // Nullable for DNS/DNC cases

        [Range(0, double.MaxValue, ErrorMessage = "Corrected time must be positive")]
        public double CorrectedTime { get; set; } // Handicap corrected (calculated)

        [Range(1, int.MaxValue, ErrorMessage = "Position must be positive")]
        public int Position { get; set; } // Finish position

        // Status codes: null = finished, "DNS", "DNC", "DNF", "RET"
        [StringLength(10, ErrorMessage = "Status cannot exceed 10 characters")]
        public string? Status { get; set; }

        // Points based on scoring system
        [Range(0, int.MaxValue, ErrorMessage = "Points must be non-negative")]
        public int Points { get; set; }

        // Handicap number (PY, IRC, YTC rating)
        [Range(0, double.MaxValue, ErrorMessage = "Handicap number must be positive")]
        public double? HandicapNumber { get; set; }

        // Audit trail
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? AmendedBy { get; set; }
        public DateTime? AmendedAt { get; set; }

        // Helper method to validate the result
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(RaceId) &&
                   !string.IsNullOrWhiteSpace(SailorName) &&
                   !string.IsNullOrWhiteSpace(SailNumber) &&
                   CorrectedTime >= 0 &&
                   Position >= 0;
        }
    }
}
