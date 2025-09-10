using SailingResultsPortal.Models;
using System;

namespace SailingResultsPortal.Services
{
    public static class ScoringService
    {
        // Portsmouth Yardstick formula to calculate corrected time
        // CorrectedTime = ElapsedTime * 1000 / PYNumber
        // PYNumber: Portsmouth Yardstick number (e.g., 1100 for a boat)
        public static double CalculatePortsmouthCorrectedTime(TimeSpan elapsedTime, double pyNumber)
        {
            if (pyNumber <= 0)
                throw new ArgumentException("PY Number must be > 0");

            double correctedSeconds = elapsedTime.TotalSeconds * 1000 / pyNumber;
            return correctedSeconds;
        }

        // IRC scoring (simplified example)
        // CorrectedTime = ElapsedTime * IRCRating
        // IRCRating typically between 0.9 and 1.3 (smaller is faster)
        public static double CalculateIRCCorrectedTime(TimeSpan elapsedTime, double ircRating)
        {
            if (ircRating <= 0)
                throw new ArgumentException("IRC Rating must be > 0");

            double correctedSeconds = elapsedTime.TotalSeconds * ircRating;
            return correctedSeconds;
        }

        // YTC scoring example (Time Correction Factor similar to IRC)
        // CorrectedTime = ElapsedTime * YTCRatingFactor
        // Placeholder example; update with real formula
        public static double CalculateYTCCorrectedTime(TimeSpan elapsedTime, double ytcRating)
        {
            if (ytcRating <= 0)
                throw new ArgumentException("YTC Rating must be > 0");

            double correctedSeconds = elapsedTime.TotalSeconds * ytcRating;
            return correctedSeconds;
        }
    }
}
