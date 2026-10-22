using System;

namespace QualityEverything
{
    internal static class QualityTier
    {
        public static int AddLevels(int tier, int offset) => (int)Math.Max(0L, Math.Min(6L, (long)tier + offset));
    }
}
