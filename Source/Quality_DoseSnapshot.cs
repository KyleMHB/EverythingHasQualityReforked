using System;

namespace ConfigurableQuality
{
    // Constant-size sufficient statistics for proportional blending of remaining native severity.
    // Native caps scale all captured contributions equally; elapsed decay retains their proportions.
    internal sealed class DoseSnapshot
    {
        public float Strength, Duration;
        public DoseSnapshot(float strength = 1, float duration = 1)
        {
            Strength = Valid(strength); Duration = Valid(duration);
        }
        public static float Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= .01f && value <= 10f ? value : 1f;
        public static DoseSnapshot Merge(DoseSnapshot oldDose, float remaining, DoseSnapshot incoming, float amount)
        {
            oldDose = oldDose ?? new DoseSnapshot(); incoming = incoming ?? new DoseSnapshot();
            remaining = Math.Max(0, remaining); amount = Math.Max(0, amount);
            float total = remaining + amount;
            return total > 0 ? new DoseSnapshot((oldDose.Strength * remaining + incoming.Strength * amount) / total,
                (oldDose.Duration * remaining + incoming.Duration * amount) / total) : new DoseSnapshot();
        }
    }
    // Difference from the native per-definition average, with untracked doses left at Normal.
    internal sealed class MedicineDoseSnapshot
    {
        public MedicineDoseSnapshot() { }
        public float Correction;
        public int Count;
        public void Record(int count, float potency, float nativePotency)
        {
            if (count <= 0) return;
            Correction += (potency - nativePotency) * count;
            Count += count;
        }
        public float Apply(float nativeAverage, int nativeCount) => nativeCount > 0 && Count > 0 && Count <= nativeCount
            && !float.IsNaN(Correction) && !float.IsInfinity(Correction)
            ? Math.Max(0, nativeAverage + Correction / nativeCount) : nativeAverage;
    }}
