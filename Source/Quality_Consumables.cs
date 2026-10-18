using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace QualityEverything
{
    internal static class QualityConsumableEffects
    {
        internal static readonly Harmony Harmony = new Harmony("rimworld.qualityframework.consumables");
        public static void Register()
        {
            AddStat(StatDefOf.Nutrition, EffectId.FoodNutrition);
            AddStat(StatDefOf.FoodPoisonChanceFixedHuman, EffectId.FoodPoisoningRisk);
            QualityMedicineEffects.Register();
            QualityDrugEffects.Register();
        }
        internal static bool Patch(MethodBase target, string prefix = null, string postfix = null, string transpiler = null, Type owner = null)
        {
            if (target == null) return false;
            owner = owner ?? typeof(QualityConsumableEffects);
            try
            {
                Harmony.Patch(target, prefix == null ? null : new HarmonyMethod(owner, prefix),
                    postfix == null ? null : new HarmonyMethod(owner, postfix), transpiler == null ? null : new HarmonyMethod(owner, transpiler));
                return true;
            }
            catch (Exception e) { Log.Error("QEverything: consumable adapter unavailable for " + target + ": " + e.Message); return false; }
        }
        internal static void AddStat(StatDef stat, EffectId effect)
        {
            bool conflict = stat?.parts?.Any(p => p is StatPart_Quality || p is QualityConsumableStatPart) == true;
            if (stat != null && !conflict)
            {
                if (stat.parts == null) stat.parts = new List<StatPart>();
                stat.parts.Add(new QualityConsumableStatPart { parentStat = stat, Effect = effect });
            }
            QualityEffects.SetCapability(effect, stat != null && !conflict,
                (conflict ? "QEverything.Consumables.StatConflict" : "QEverything.Consumables.StatSupported").Translate().ToString());
        }
    }

    internal sealed class QualityConsumableStatPart : StatPart
    {
        internal EffectId Effect;
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && Eligible(req.Thing)) val *= QualityEffects.Factor(Effect, req.Thing);
        }
        bool Eligible(Thing thing) => Effect == EffectId.MedicinePotency ? thing.def.IsMedicine : thing.def.ingestible != null;
        public override string ExplanationPart(StatRequest req)
        {
            float factor = req.HasThing && Eligible(req.Thing) ? QualityEffects.Factor(Effect, req.Thing) : 1;
            return factor == 1 ? null : "QEverything.Consumables.StatFactor".Translate(factor.ToStringPercent()).ToString();
        }
    }

    internal static class QualityMedicineEffects
    {
        static readonly ConditionalWeakTable<Bill_Medical, MedicineDoseSnapshot> doses = new ConditionalWeakTable<Bill_Medical, MedicineDoseSnapshot>();
        static bool tendReady, surgeryReady;
        static readonly MethodInfo nativeTend = AccessTools.Method(typeof(TendUtility), "CalculateBaseTendQuality", new[] { typeof(Pawn), typeof(Pawn), typeof(ThingDef) });
        public static void Register()
        {
            var owner = typeof(QualityMedicineEffects);
            tendReady = QualityConsumableEffects.Patch(AccessTools.Method(typeof(TendUtility), "DoTend"), transpiler: nameof(TendTranspiler), owner: owner)
                && QualityPatchUtility.Statuses.TryGetValue("medicine tending", out bool tendMatched) && tendMatched;
            // Both supported versions expose this native outcome component. Resolve it before installing adapters.
            var outcome = AccessTools.TypeByName("RimWorld.SurgeryOutcomeComp_MedicineQuality");
            surgeryReady = outcome != null
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(Bill_Medical), "Notify_BillWorkStarted"), prefix: nameof(BeginBill), transpiler: nameof(InitialMedicineTranspiler), owner: owner)
                && QualityPatchUtility.Statuses.TryGetValue("surgery initial medicine", out bool initialMatched) && initialMatched
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(Toils_Recipe), "CalculateIngredients"), postfix: nameof(Ingredients), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.Method(outcome, "XGetter"), postfix: nameof(SurgeryPotency), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(Bill_Medical), "ExposeData"), postfix: nameof(ExposeBill), owner: owner)
                && QualityConsumableEffects.Patch(AccessTools.Method(typeof(Bill_Medical), "Clone"), postfix: nameof(CloneBill), owner: owner);
            if (tendReady && surgeryReady)
            {
                QualityConsumableEffects.AddStat(StatDefOf.MedicalPotency, EffectId.MedicinePotency);
                if (QualityEffects.Available(EffectId.MedicinePotency)) QualityEffects.SetCapability(EffectId.MedicinePotency, true, "QEverything.Consumables.MedicineSupported".Translate().ToString());
            }
            else QualityEffects.SetCapability(EffectId.MedicinePotency, false, "QEverything.Consumables.MedicineUnavailable".Translate().ToString());
        }
        static IEnumerable<CodeInstruction> TendTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var result = QualityPatchUtility.ReplaceSingleCall(instructions, nativeTend, AccessTools.Method(typeof(QualityMedicineEffects), nameof(Tend)),
                () => new[] { new CodeInstruction(OpCodes.Ldarg_2) }, "medicine tending");
            tendReady = QualityPatchUtility.Statuses["medicine tending"];
            return result;
        }
        static float Tend(Pawn doctor, Pawn patient, ThingDef def, Medicine medicine)
        {
            if (medicine == null || QualityEffects.Factor(EffectId.MedicinePotency, medicine) == 1) return TendUtility.CalculateBaseTendQuality(doctor, patient, def);
            return TendUtility.CalculateBaseTendQuality(doctor, patient, medicine.GetStatValue(StatDefOf.MedicalPotency), def.GetStatValueAbstract(StatDefOf.MedicalQualityMax));
        }
        static void BeginBill(Bill_Medical __instance) => doses.Remove(__instance);
        static IEnumerable<CodeInstruction> InitialMedicineTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var result = QualityPatchUtility.ReplaceSingleCall(instructions, AccessTools.Method(typeof(RecipeWorker), "ConsumeIngredient"),
                AccessTools.Method(typeof(QualityMedicineEffects), nameof(ConsumeInitial)), () => new[] { new CodeInstruction(OpCodes.Ldarg_0) }, "surgery initial medicine");
            surgeryReady = QualityPatchUtility.Statuses["surgery initial medicine"];
            return result;
        }
        static void ConsumeInitial(RecipeWorker worker, Thing ingredient, RecipeDef recipe, Map map, Bill_Medical bill)
        {
            float correction = Correction(ingredient);
            int count = ingredient.stackCount;
            worker.ConsumeIngredient(ingredient, recipe, map);
            var state = doses.GetOrCreateValue(bill);
            state.Record(count, correction, 0);
        }
        static float Correction(Thing thing) => thing?.def.IsMedicine == true && QualityEffects.Factor(EffectId.MedicinePotency, thing) != 1
            ? thing.GetStatValue(StatDefOf.MedicalPotency) - thing.def.GetStatValueAbstract(StatDefOf.MedicalPotency) : 0;
        static void Ingredients(Job job, List<Thing> __result)
        {
            if (!(job.bill is Bill_Medical bill) || __result == null) return;
            foreach (var thing in __result) if (thing != null && thing.def.IsMedicine)
                {
                    doses.GetOrCreateValue(bill).Record(thing.stackCount, Correction(thing), 0);
                }
        }
        static void SurgeryPotency(Bill bill, ref float __result)
        {
            if (!QualityEffects.Available(EffectId.MedicinePotency) || !(bill is Bill_Medical medical) || !doses.TryGetValue(medical, out var dose)) return;
            int count = medical.consumedMedicine?.Values.Sum() ?? 0;
            __result = dose.Apply(__result, count);
        }
        static void ExposeBill(Bill_Medical __instance)
        {
            var dose = doses.GetOrCreateValue(__instance);
            Scribe_Values.Look(ref dose.Correction, "qEverythingMedicineCorrection", 0f);
            Scribe_Values.Look(ref dose.Count, "qEverythingMedicineCount", 0);
            if (float.IsNaN(dose.Correction) || float.IsInfinity(dose.Correction) || dose.Count < 0) { dose.Correction = 0; dose.Count = 0; }
        }
        static void CloneBill(Bill_Medical __instance, Bill __result)
        {
            if (__result is Bill_Medical clone && doses.TryGetValue(__instance, out var dose))
            {
                var copy = doses.GetOrCreateValue(clone); copy.Correction = dose.Correction; copy.Count = dose.Count;
            }
        }
    }
}
