using System;
namespace ConfigurableQuality
{
    // Shared condition seam: creation/real quality changes preserve ratio; loading preserves absolute HP.
    internal static class QualityCondition
    {
        public static int Remap(int hitPoints, int oldMaximum, int newMaximum)
        {
            if (hitPoints <= 0 || oldMaximum <= 0 || newMaximum <= 0) return hitPoints;
            double ratio = Math.Min(1d, (double)hitPoints / oldMaximum);
            return Math.Max(1, Math.Min(newMaximum, (int)Math.Round(ratio * newMaximum)));
        }
        public static int ClampLoaded(int hitPoints, int maximum) => maximum > 0 && hitPoints > maximum ? maximum : hitPoints;
    }
}
