// File: Models/HandicapFilter.cs
using System;

namespace SailingResultsPortal.Models
{
    /// <summary>
    /// Represents a user-defined handicap filter row:
    /// - ClassName: the class to match (by name, e.g., "Laser", "Cruiser")
    /// - Inequality: "<", "=", ">", or "between"
    /// - HandicapType: "PY", "IRC", or "YTC" (used for labeling and future expansion)
    /// - Value1/Value2: numeric values to compare against the class rating (Value2 used only for "between")
    /// </summary>
    public class HandicapFilter
    {
        public string ClassName { get; set; } = string.Empty;
        public string Inequality { get; set; } = "=";
        public string HandicapType { get; set; } = "";
        public double? Value1 { get; set; }
        public double? Value2 { get; set; }
    }
}
