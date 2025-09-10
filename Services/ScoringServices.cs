using System;

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
    }
}
