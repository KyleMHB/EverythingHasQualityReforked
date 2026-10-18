using System;
using QualityEverything;
class ConsumableChecks
{
    static void Near(float actual, float expected) { if (Math.Abs(actual - expected) > .0001f) throw new Exception($"Expected {expected}, got {actual}"); }
    static void Main()
    {
        // A half-severity old dose and quarter-severity new dose retain both snapshots.
        var mixed = DoseSnapshot.Merge(new DoseSnapshot(1.2f, 1.2f), .5f, new DoseSnapshot(.95f, .95f), .25f);
        Near(mixed.Strength, 1.1166667f); Near(mixed.Duration, 1.1166667f);
        // Swapping simultaneously administered doses cannot replace earlier captured factors.
        var reverse = DoseSnapshot.Merge(new DoseSnapshot(.95f, .95f), .25f, new DoseSnapshot(1.2f, 1.2f), .5f);
        Near(reverse.Strength, 1.1166667f); Near(reverse.Duration, 1.1166667f);
        // A capped full high retains proportional old/new contributions, without amplifying either.
        var capped = DoseSnapshot.Merge(new DoseSnapshot(1.2f, 1.2f), 1f, new DoseSnapshot(.95f, .95f), .5f);
        Near(capped.Strength, 1.1166667f);
        // Half the old high has decayed before a fresh low-quality dose: only remaining severity weighs in.
        var later = DoseSnapshot.Merge(new DoseSnapshot(1.2f, 1.2f), .25f, new DoseSnapshot(.95f, .95f), .5f);
        Near(later.Strength, 1.0333333f);
        // Existing untracked and newly disabled doses enter with Normal factors.
        var oldSave = DoseSnapshot.Merge(null, .5f, new DoseSnapshot(1.2f, 1.2f), .5f);
        Near(oldSave.Strength, 1.1f);
        var disabled = DoseSnapshot.Merge(new DoseSnapshot(1.2f, 1.2f), .5f, null, .5f);
        Near(disabled.Duration, 1.1f);
        Near(new DoseSnapshot(float.NaN, float.PositiveInfinity).Strength, 1);
        Near(new DoseSnapshot(-1, float.PositiveInfinity).Duration, 1);
        var medicine = new MedicineDoseSnapshot();
        medicine.Record(1, 1.2f, 1f);
        Near(medicine.Apply(1f, 1), 1.2f);
        // A saved first dose plus two old/untracked doses leaves those two at native potency.
        var resumed = new MedicineDoseSnapshot { Count = medicine.Count, Correction = medicine.Correction };
        Near(resumed.Apply(1f, 3), 1.0666667f);
        resumed.Record(2, .6f, .5f);
        Near(resumed.Apply(.6666667f, 3), .8f);
        // Never guess a correction if captured quantities exceed native bookkeeping.
        Near(resumed.Apply(.5f, 1), .5f);
        Console.WriteLine("Consumable checks passed (mixed doses, cap, decay, disabled/untracked, invalid snapshots, surgical dose bookkeeping)");
    }
}
