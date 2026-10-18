using System;
namespace QualityEverything
{
    internal static class QualityEnergyCalculation
    {
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static float ClampStored(float stored, float capacity) => Math.Min(stored, capacity);
        public static float Efficiency(float raw, float factor) => Math.Max(0f, Math.Min(1f, raw * factor));
        public static float Acceptance(float capacity, float stored, float efficiency) => efficiency > 0f ? Math.Max(0f, capacity - stored) / efficiency : 0f;
    }
}
