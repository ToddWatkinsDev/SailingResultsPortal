using System;
using System.Text.RegularExpressions;
using SailingResultsPortal.Models;

namespace SailingResultsPortal.Services
{
    public static class HandicapFilterParser
    {
        // Regex to match the filter format: [Class][Inequality][Value(s)][HandicapType]
        // Examples: Laser<1000PY, Cruiser=1.2IRC, Dinghy between 900 and 1100 PY
        private static readonly Regex FilterRegex = new Regex(
            @"^(?<class>[^\<\=\>]+)(?<inequality>\<|\=|\>|\s+between\s+)(?<values>[\d\.\s]+(?:\s+and\s+[\d\.\s]+)?)(?<type>PY|IRC|YTC)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public static HandicapFilter Parse(string filterString)
        {
            if (string.IsNullOrWhiteSpace(filterString))
                throw new ArgumentException("Filter string cannot be empty");

            var match = FilterRegex.Match(filterString.Trim());
            if (!match.Success)
                throw new ArgumentException($"Invalid filter format: {filterString}. Expected format: Class<Inequality>Value(s)HandicapType");

            var filter = new HandicapFilter
            {
                ClassName = match.Groups["class"].Value.Trim(),
                HandicapType = match.Groups["type"].Value.ToUpper()
            };

            var ineq = match.Groups["inequality"].Value.Trim().ToLower();
            if (ineq == "<") filter.Inequality = "<";
            else if (ineq == "=") filter.Inequality = "=";
            else if (ineq == ">") filter.Inequality = ">";
            else if (ineq == "between") filter.Inequality = "between";
            else throw new ArgumentException("Invalid inequality operator");

            var values = match.Groups["values"].Value.Trim();
            if (filter.Inequality == "between")
            {
                var parts = values.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new ArgumentException("Between requires two values separated by 'and'");

                if (!double.TryParse(parts[0].Trim(), out var val1) || !double.TryParse(parts[1].Trim(), out var val2))
                    throw new ArgumentException("Invalid numeric values for between");

                filter.Value1 = val1;
                filter.Value2 = val2;
            }
            else
            {
                if (!double.TryParse(values, out var val))
                    throw new ArgumentException("Invalid numeric value");

                filter.Value1 = val;
            }

            return filter;
        }
    }
}
