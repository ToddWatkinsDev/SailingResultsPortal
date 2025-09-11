using System;
using SailingResultsPortal.Models;

namespace SailingResultsPortal.Services
{
    public static class ScoringService
    {
        // Portsmouth Yardstick formula to calculate corrected time as TimeSpan
        // CorrectedTime = ElapsedTime * 1000 / PYNumber
        public static TimeSpan CalculatePortsmouthCorrectedTime(TimeSpan elapsedTime, double pyNumber)
        {
            if (pyNumber <= 0)
                throw new ArgumentException("PY Number must be > 0");

            double correctedSeconds = elapsedTime.TotalSeconds * 1000 / pyNumber;
            return TimeSpan.FromSeconds(correctedSeconds);
        }

        // IRC scoring (simplified example) returning TimeSpan
        // CorrectedTime = ElapsedTime * IRCRating
        public static TimeSpan CalculateIRCCorrectedTime(TimeSpan elapsedTime, double ircRating)
        {
            if (ircRating <= 0)
                throw new ArgumentException("IRC Rating must be > 0");

            double correctedSeconds = elapsedTime.TotalSeconds * ircRating;
            return TimeSpan.FromSeconds(correctedSeconds);
        }

        // YTC scoring example (simplified placeholder) returning TimeSpan
        // CorrectedTime = ElapsedTime * YTCRatingFactor
        public static TimeSpan CalculateYTCCorrectedTime(TimeSpan elapsedTime, double ytcRating)
        {
            if (ytcRating <= 0)
                throw new ArgumentException("YTC Rating must be > 0");

            double correctedSeconds = elapsedTime.TotalSeconds * ytcRating;
            return TimeSpan.FromSeconds(correctedSeconds);
        }

        // Points calculation methods
        public static int CalculatePoints(int position, int totalBoats, string scoringSystem, bool isMedalRace = false)
        {
            int basePoints;

            switch (scoringSystem.ToLower())
            {
                case "lowpoint":
                    basePoints = position;
                    break;
                case "highpoint":
                    basePoints = totalBoats - position + 1;
                    break;
                default:
                    basePoints = position; // Default to low point
                    break;
            }

            // Double points for medal races
            if (isMedalRace)
            {
                basePoints *= 2;
            }

            return basePoints;
        }

        // Handle DNS/DNC/DNF/RET points
        public static int CalculatePenaltyPoints(string status, int totalBoats, string scoringSystem)
        {
            switch (status?.ToUpper())
            {
                case "DNS": // Did Not Start
                case "DNC": // Did Not Compete
                    return scoringSystem.ToLower() == "highpoint" ? 0 : totalBoats + 1;
                case "DNF": // Did Not Finish
                case "RET": // Retired
                    return scoringSystem.ToLower() == "highpoint" ? 1 : totalBoats + 1;
                default:
                    return 0; // No penalty
            }
        }

        // Calculate points for all results in a race
        public static void CalculateRacePoints(List<Result> raceResults, Race race)
        {
            var finishedBoats = raceResults.Where(r => r.Status == null).OrderBy(r => r.CorrectedTime).ToList();
            var totalBoats = raceResults.Count;

            foreach (var result in raceResults)
            {
                if (result.Status != null)
                {
                    // Handle penalty cases
                    result.Points = CalculatePenaltyPoints(result.Status, totalBoats, race.ScoringSystem);
                }
                else
                {
                    // Find position among finished boats
                    var position = finishedBoats.FindIndex(r => r.Id == result.Id) + 1;
                    result.Points = CalculatePoints(position, finishedBoats.Count, race.ScoringSystem, race.IsMedalRace);
                }
            }
        }

        // Calculate overall points for a sailor with discards applied
        public static int CalculateOverallPointsWithDiscards(List<int> racePoints, int racesBeforeDiscards, int numberOfDiscards)
        {
            if (racesBeforeDiscards == 0 || numberOfDiscards == 0 || racePoints.Count <= racesBeforeDiscards)
            {
                // No discards or not enough races yet
                return racePoints.Sum();
            }

            // Sort points in descending order (worst first)
            var sortedPoints = racePoints.OrderByDescending(p => p).ToList();

            // Discard the worst races
            var pointsToUse = sortedPoints.Skip(numberOfDiscards).ToList();

            return pointsToUse.Sum();
        }
    }
}
