namespace ConfigurableQuality
{
    // Curves describe benefit strength. The direction belongs to the stat, not the sign of its label.
    internal static class QualityGearContribution
    {
        public static float Scale(float contribution, float factor, bool lowerIsBetter)
        {
            if (float.IsNaN(contribution) || float.IsInfinity(contribution) ||
                float.IsNaN(factor) || float.IsInfinity(factor) || factor <= 0f) return contribution;
            if (lowerIsBetter ? contribution >= 0f : contribution <= 0f) return contribution;
            return contribution * factor;
        }
    }
}
