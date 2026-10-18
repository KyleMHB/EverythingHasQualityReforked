using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace QualityEverything
{
    [HarmonyPatch]
    public static class Quality_Construction
    {
        [HarmonyPatch(typeof(Frame), "CompleteConstruction")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ConstructionQuality(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var original = AccessTools.Method(typeof(QualityUtility), "GenerateQualityCreatedByPawn", new[] { typeof(Pawn), typeof(SkillDef), typeof(bool) });
            var source = instructions.ToList();
            if (original == null || source.Count(i => i.Calls(original)) != 1)
            {
                Log.Error("QEverything: construction quality pattern did not match exactly once; leaving original behavior intact.");
                return source;
            }
            LocalBuilder materialQuality = generator.DeclareLocal(typeof(int));
            var result = QualityPatchUtility.ReplaceSingleCall(source, original,
                AccessTools.Method(typeof(Quality_Construction), nameof(GenerateConstructionQuality)),
                () => new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc, materialQuality) }, "construction quality");
            // Capture before vanilla destroys the frame's resource container.
            result.InsertRange(0, new[] {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Quality_Construction), nameof(CalculateMaterialQuality))),
                new CodeInstruction(OpCodes.Stloc, materialQuality) });
            return result;
        }

        public static QualityCategory GenerateConstructionQuality(Pawn pawn, SkillDef skill, bool consumeInspiration, Frame frame, int materialQuality)
        {
            var builtDef = frame.def.entityDefToBuild as ThingDef;
            return Quality_Generator.GenerateQualityForDef(pawn, skill, builtDef, materialQuality, null, consumeInspiration);
        }
        public static int CalculateMaterialQuality(Frame frame)
        {
            float materialQuality = -1;
            ThingDef thingDef = frame.def.entityDefToBuild as ThingDef;
            if (ModSettings_QEverything.useMaterialQuality && thingDef != null && QualityDefUtility.ShouldReceiveQuality(thingDef) && thingDef.HasComp(typeof(CompQuality)))
            {
                int numIng = 0;
                for (int i = 0; i < frame.resourceContainer.Count; i++)
                {
                    Thing resource = frame.resourceContainer[i];
                    CompQuality comp = resource.TryGetComp<CompQuality>();
                    if (comp != null)
                    {
                        //Log.Message("resource " + i + " is " + (int)comp.Quality);
                        //Log.Message("resource " + i + " has " + resource.stackCount);
                        materialQuality += (int)comp.Quality * resource.stackCount;
                        numIng += resource.stackCount;
                    }
                }
                if (numIng > 0)
                {
                    materialQuality = (materialQuality + 1) / numIng;
                    //Log.Message("Value is " + materialQuality.ToString() + " for " + numIng + " resources");
                }
            }
            return GenMath.RoundRandom(materialQuality);
        }

        //Prevents pawns from picking up stacks that can't stack when gathering construction materials
        public static IEnumerable<CodeInstruction> CollectNextTarget_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var defField = AccessTools.Field(typeof(Thing), nameof(Thing.def));
            var matches = new List<Tuple<int, int>>();
            for (int second = 1; second + 1 < list.Count; second++)
            {
                if (!list[second].LoadsField(defField) ||
                    (list[second + 1].opcode != OpCodes.Bne_Un && list[second + 1].opcode != OpCodes.Bne_Un_S)) continue;
                for (int first = second - 1; first >= Math.Max(0, second - 8); first--)
                {
                    if (list[first].opcode.FlowControl == FlowControl.Branch || list[first].opcode.FlowControl == FlowControl.Cond_Branch) break;
                    if (!list[first].LoadsField(defField)) continue;
                    matches.Add(Tuple.Create(first, second));
                    break;
                }
            }
            if (matches.Count != 1)
            {
                Log.Error("QEverything: haul stack comparison pattern did not match exactly once; leaving original behavior intact.");
                return list;
            }
            int left = matches[0].Item1;
            int right = matches[0].Item2;
            list[left].opcode = OpCodes.Nop;
            list[left].operand = null;
            list[right].opcode = OpCodes.Callvirt;
            list[right].operand = AccessTools.Method(typeof(Thing), nameof(Thing.CanStackWith));
            list[right + 1].opcode = OpCodes.Brfalse;            return list;
        }
    }
}
