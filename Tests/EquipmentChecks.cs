using System;
using QualityEverything;
class EquipmentChecks
{
    static void Check(float actual, float expected, string name)
    {
        if (Math.Abs(actual - expected) > .00001f) throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    static void Main()
    {
        Check(QualityGearContribution.Scale(0f, 1.2f, false), 0f, "zero gear grants no bonus");
        Check(QualityGearContribution.Scale(.2f, 1.2f, false), .24f, "legendary scales only the existing contribution");
        Check(QualityGearContribution.Scale(.2f, .95f, false), .19f, "awful reduces an existing benefit");
        Check(QualityGearContribution.Scale(-.2f, 1.2f, false), -.2f, "ordinary negative penalty is preserved");
        Check(QualityGearContribution.Scale(-10f, 1.2f, true), -12f, "lower temperature limit benefit improves downward");
        Check(QualityGearContribution.Scale(-10f, .95f, true), -9.5f, "awful weakens lower-is-better benefit");
        Check(QualityGearContribution.Scale(10f, 1.2f, true), 10f, "lower-is-better positive penalty is preserved");
        Check(QualityGearContribution.Scale(.2f, 1f, false), .2f, "normal remains neutral");
        Check(QualityGearContribution.Scale(.2f, 0f, false), .2f, "invalid factor cannot erase contribution");
        Console.WriteLine("Equipment checks passed: 9 contribution calculations.");
    }
}
