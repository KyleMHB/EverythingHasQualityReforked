using System;
using QualityEverything;
class DurabilityChecks
{
    static void Check(int actual, int expected, string name) { if (actual != expected) throw new Exception(name + ": expected " + expected + ", got " + actual); }
    static void Main()
    {
        Check(QualityCondition.Remap(50, 100, 120), 60, "genuine upgrade preserves half condition");
        Check(QualityCondition.Remap(100, 100, 95), 95, "fresh full condition survives pre-comp baseline normalization");
        Check(QualityCondition.Remap(40, 100, 95), 38, "fresh randomly damaged item retains condition");
        Check(QualityCondition.Remap(60, 120, 100), 50, "downgrade preserves condition");
        Check(QualityCondition.Remap(50, 100, 100), 50, "same maximum leaves damage unchanged");
        Check(QualityCondition.Remap(0, 100, 120), 0, "quality change cannot resurrect destroyed item");
        Check(QualityCondition.Remap(1, 1000, 10), 1, "rounded ratio cannot kill living item");
        Check(QualityCondition.Remap(110, 100, 120), 120, "invalid excess condition cannot exceed new maximum");
        Check(QualityCondition.Remap(40, 0, 120), 40, "unknown creation baseline cannot grant HP");
        Check(QualityCondition.ClampLoaded(50, 120), 50, "settings increase cannot heal saved damage");
        Check(QualityCondition.ClampLoaded(110, 100), 100, "settings decrease clamps absolute HP");
        Check(QualityCondition.ClampLoaded(50, 100), 50, "settings disable cannot preserve percentage and heal");
        Check(QualityCondition.ClampLoaded(0, 120), 0, "loading cannot resurrect item");
        Console.WriteLine("Durability checks passed: 13 condition transitions.");
    }
}
