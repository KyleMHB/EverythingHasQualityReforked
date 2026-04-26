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

        [HarmonyPatch(
            typeof(Mineable),
            "TrySpawnYield",
            new Type[] { typeof(Map), typeof(float), typeof(bool), typeof(Pawn) })]
        [HarmonyTranspiler]       
        static IEnumerable<CodeInstruction> MiningQuality(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = instructions.ToList<CodeInstruction>();
            CodeInstruction code;
            for (int i = 0; i < codeInstructions.Count; i++)
            {
                code = codeInstructions[i];
                yield return code;
                if (code.opcode == OpCodes.Call && (MethodInfo)code.operand == AccessTools.Method(typeof(ThingMaker), "MakeThing", new Type[] { typeof(ThingDef), typeof(ThingDef) }))
                {
                    CodeInstruction loadInstruction = CodeInstruction.LoadField(typeof(SkillDefOf), "Mining");
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return loadInstruction;
                    yield return new CodeInstruction(OpCodes.Call, mGenerateResQuality);
                }
            }
            yield break;
            //Log.Message("Mining patch successful");
            //return InsertQualityCall(instructions);
        }

        [HarmonyPatch(typeof(CompHasGatherableBodyResource), "Gathered")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> AnimalQuality(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = instructions.ToList<CodeInstruction>();
            CodeInstruction code;
            for (int i = 0; i < codeInstructions.Count; i++)
            {
                code = codeInstructions[i];
                yield return code;
                if (code.opcode == OpCodes.Call && (MethodInfo)code.operand == AccessTools.Method(typeof(ThingMaker), "MakeThing", new Type[] { typeof(ThingDef), typeof(ThingDef) }))
                {
                    CodeInstruction loadInstruction = CodeInstruction.LoadField(typeof(SkillDefOf), "Animals");
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return loadInstruction;
                    yield return new CodeInstruction(OpCodes.Call, mGenerateResQuality);
                }
            }
            yield break;
            //return InsertQualityCall(instructions);
        }

        [HarmonyPatch(typeof(CompEggLayer), "ProduceEgg")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> EggQuality(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = instructions.ToList<CodeInstruction>();
            CodeInstruction code;
            for (int i = 0; i < codeInstructions.Count; i++)
            {
                code = codeInstructions[i];
                yield return code;
                if (code.opcode == OpCodes.Call && (MethodInfo)code.operand == AccessTools.Method(typeof(ThingMaker), "MakeThing", new Type[] { typeof(ThingDef), typeof(ThingDef) }))
                {
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Call, mGenerateResQuality);
                }
            }
            yield break;
        }

        public static void GenerateResourceQuality(Thing thing, Pawn worker = null, SkillDef relevantSkill = null)
        {
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
