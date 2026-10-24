using System;
using ConfigurableQuality;
class EnergyChecks
{
    static void Equal(float actual, float expected, string name) { if (Math.Abs(actual - expected) > .001f || float.IsNaN(actual)) throw new Exception(name + ": " + actual); }
    static void Main()
    {
        Equal(QualityEnergyCalculation.ClampStored(420, 900), 420, "capacity increase preserves absolute charge");
        Equal(QualityEnergyCalculation.ClampStored(700, 450), 450, "capacity decrease discards only excess");
        Equal(QualityEnergyCalculation.Efficiency(.9f, 1.5f), 1, "charging cannot create energy");
        Equal(QualityEnergyCalculation.Efficiency(.5f, .8f), .4f, "efficiency independent of stored energy");
        Equal(QualityEnergyCalculation.Acceptance(900, 420, .8f), 600, "input energy accounts for conversion loss");
        Equal(QualityEnergyCalculation.Acceptance(450, 700, .8f), 0, "overfull battery accepts no energy");
        Equal(QualityEnergyCalculation.Acceptance(900, 420, 0), 0, "zero efficiency accepts no energy");
        Console.WriteLine("7 energy state checks passed.");
    }
}
