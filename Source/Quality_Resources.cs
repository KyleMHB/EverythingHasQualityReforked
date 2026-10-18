using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using HarmonyLib;

namespace QualityEverything
{
    [HarmonyPatch]
    class Quality_Resources
    {
        static readonly MethodInfo mGenerateResQuality = AccessTools.Method(typeof(Quality_Resources), "GenerateResourceQuality");

        [HarmonyPatch(typeof(QuestManager), "Notify_PlantHarvested")]
        [HarmonyPrefix]
        public static void HarvestQuality(Pawn worker, Thing harvested)
        {
            GenerateResourceQuality(harvested, worker, SkillDefOf.Plants);
        }

        [HarmonyPatch(typeof(Mineable), "TrySpawnYield", new Type[] { typeof(Map), typeof(bool), typeof(Pawn) })]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> MiningQuality(IEnumerable<CodeInstruction> instructions)
        {
            return InsertResourceQuality(instructions, 1, "mining", () => new[] {
                new CodeInstruction(OpCodes.Ldarg_3), CodeInstruction.LoadField(typeof(SkillDefOf), nameof(SkillDefOf.Mining)) });
        }

        [HarmonyPatch(typeof(CompHasGatherableBodyResource), "Gathered")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> AnimalQuality(IEnumerable<CodeInstruction> instructions)
        {
            return InsertResourceQuality(instructions, 1, "animal gathering", () => new[] {
                new CodeInstruction(OpCodes.Ldarg_1), CodeInstruction.LoadField(typeof(SkillDefOf), nameof(SkillDefOf.Animals)) });
        }

        [HarmonyPatch(typeof(CompEggLayer), "ProduceEgg")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> EggQuality(IEnumerable<CodeInstruction> instructions)
        {
            return InsertResourceQuality(instructions, 2, "egg laying", () => new[] {
                new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Ldnull) });
        }

        private static IEnumerable<CodeInstruction> InsertResourceQuality(IEnumerable<CodeInstruction> instructions, int expected, string feature, Func<IEnumerable<CodeInstruction>> workerAndSkill)
        {
            var code = instructions.ToList();
            var makeThing = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing), new[] { typeof(ThingDef), typeof(ThingDef) });
            if (code.Count(i => i.Calls(makeThing)) != expected)
            {
                Log.Error("QEverything: " + feature + " creation pattern did not match expected count; leaving original behavior intact.");
                return code;
            }
            var result = new List<CodeInstruction>();
            foreach (var instruction in code)
            {
                if (!instruction.Calls(makeThing)) { result.Add(instruction); continue; }
                var copy = new CodeInstruction(instruction);
                var call = new CodeInstruction(OpCodes.Call, mGenerateResQuality);
                // Keep the generated callback inside the same protected region as creation.
                call.blocks.AddRange(copy.blocks.Where(b => b.blockType == ExceptionBlockType.EndExceptionBlock));
                copy.blocks.RemoveAll(b => b.blockType == ExceptionBlockType.EndExceptionBlock);
                result.Add(copy);
                result.Add(new CodeInstruction(OpCodes.Dup));
                result.AddRange(workerAndSkill());
                result.Add(call);
            }
            return result;
        }
        public static void GenerateResourceQuality(Thing thing, Pawn worker = null, SkillDef relevantSkill = null)
        {
            if (thing == null || !QualityDefUtility.ShouldReceiveQuality(thing.def)) return;
            CompQuality comp = thing.TryGetComp<CompQuality>();
            if (comp == null)
            {
                return;
            }

            int minQuality = QualityDefUtility.GetMinQuality(thing.def);
            int maxQuality = QualityDefUtility.GetMaxQuality(thing.def);
            QualityCategory qc;
            if (worker != null && relevantSkill != null &&
               ((relevantSkill == SkillDefOf.Mining && ModSettings_QEverything.skilledMining) || (relevantSkill == SkillDefOf.Plants && ModSettings_QEverything.skilledHarvesting) || (relevantSkill == SkillDefOf.Animals && ModSettings_QEverything.skilledAnimals)))
            {
                qc = Quality_Generator.GenerateQualityCreatedByPawn(worker, relevantSkill, thing);
            }
            else
            {
                int quality = (int)QualityUtility.GenerateQuality(QualityGenerator.BaseGen);
                quality = quality < minQuality ? minQuality : quality;
                quality = quality > maxQuality ? maxQuality : quality;
                qc = (QualityCategory)quality;
            }
            comp.SetQuality(qc, ArtGenerationContext.Colony);
        }
    }
}
