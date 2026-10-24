using System;
using ConfigurableQuality;
using HarmonyLib;
using System.Runtime.CompilerServices;
class EquipmentChecks
{
    static void Check(float actual, float expected, string name)
    {
        if (Math.Abs(actual - expected) > .00001f) throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int QualityChangeFixture(int quality) => quality;
    public static void Observer() { }

    static void CheckAdapterComposition()
    {
        var target = typeof(EquipmentChecks).GetMethod(nameof(QualityChangeFixture));
        string[] internalOwners = { "kylemhb.configurablequality", "kylemhb.configurablequality.energy", "kylemhb.configurablequality.consumables", "kylemhb.configurablequality.furniture", "kylemhb.configurablequality.native-output" };
        string[] foreignOwners = { "other.quality.provider", "kylemhb.configurablequality.unknown" };
        try
        {
            foreach (string owner in internalOwners)
                new Harmony(owner).Patch(target, postfix: new HarmonyMethod(typeof(EquipmentChecks), nameof(Observer)));
            if (QualityPatchOwnership.HasExternalPatches(target))
                throw new Exception("Internal adapters must compose without disabling equipment/combat support.");
            foreach (string owner in foreignOwners)
            {
                var foreign = new Harmony(owner);
                foreign.Patch(target, postfix: new HarmonyMethod(typeof(EquipmentChecks), nameof(Observer)));
                if (!QualityPatchOwnership.HasExternalPatches(target))
                    throw new Exception("An unknown provider must retain the conservative conflict gate: " + owner);
                foreign.Unpatch(target, HarmonyPatchType.All, owner);
            }
        }
        finally
        {
            foreach (string owner in internalOwners) new Harmony(owner).Unpatch(target, HarmonyPatchType.All, owner);
            foreach (string owner in foreignOwners) new Harmony(owner).Unpatch(target, HarmonyPatchType.All, owner);
        }
        Console.WriteLine("Adapter composition check passed: internal patches accepted; foreign providers rejected.");
    }

    static void Main()
    {
        CheckAdapterComposition();
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
