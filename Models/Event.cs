using System.Collections.Generic;

namespace SailingResultsPortal.Models
{
    public class Event
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<Race> Races { get; set; } = new();
        public List<string> TeamMembers { get; set; } = new(); // User IDs of team members
        public bool IsPublic { get; set; } = true; // Whether results are publicly viewable
        public List<string> AllowedUsers { get; set; } = new(); // User IDs allowed to view private events

        // Default scoring options for the event
        public string DefaultScoringSystem { get; set; } = "LowPoint"; // "LowPoint" or "HighPoint"
        public bool IncludeOverallByDefault { get; set; } = true; // Whether to include overall class by default
        public List<Class> DefaultClasses { get; set; } = new(); // Default classes for the event

        // Discard options for overall scoring
        public int RacesBeforeDiscards { get; set; } = 0; // Number of races before discards can be used (0 = no discards)
        public int NumberOfDiscards { get; set; } = 0; // Number of worst races to discard
    }

    public class Class
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string ScoringMethod { get; set; } = string.Empty;
        public double Rating { get; set; } = 1.0;
    }
}
