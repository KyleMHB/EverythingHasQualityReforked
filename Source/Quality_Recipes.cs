using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace ConfigurableQuality
{
    [HarmonyPatch]
    class Quality_Recipes
    {
        private static readonly QualityOperationContext<int> supply = new QualityOperationContext<int>(-1);
        public static int SupplyQuality => supply.Current;

        [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RecipeQuality(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var original = AccessTools.Method(typeof(QualityUtility), "GenerateQualityCreatedByPawn", new[] { typeof(Pawn), typeof(SkillDef), typeof(bool) });
            var error = AccessTools.Method(typeof(Log), nameof(Log.Error), new[] { typeof(string) });
            if (original == null || code.FindAll(i => i.Calls(original)).Count != 1 || code.FindAll(i => i.Calls(error)).Count != 1)
            {
                Log.Error("QEverything: recipe quality/skill diagnostic pattern did not match exactly once; leaving original behavior intact.");
                return code;
            }
            var result = QualityPatchUtility.ReplaceSingleCall(code, original,                AccessTools.Method(typeof(Quality_Recipes), nameof(GenerateRecipeQuality)),
                () => new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1) }, "recipe quality");
            // Skill-less recipes are legitimate when this mod adds quality to their products.
            return QualityPatchUtility.ReplaceSingleCall(result,
                AccessTools.Method(typeof(Log), nameof(Log.Error), new[] { typeof(string) }),
                AccessTools.Method(typeof(Quality_Recipes), nameof(ReportMissingSkill)),
                () => new[] { new CodeInstruction(OpCodes.Ldarg_0) }, "recipe skill diagnostic");
        }

        public static void ReportMissingSkill(string message, Thing product)
        {
            if (!QualityDefUtility.ShouldReceiveQuality(product.def)) Log.Error(message);
        }

        public static QualityCategory GenerateRecipeQuality(Pawn pawn, SkillDef skill, bool consumeInspiration, Thing product, RecipeDef recipe)
        {
            return Quality_Generator.GenerateQualityForDef(pawn, skill, product.def, SupplyQuality, recipe.skillRequirements, consumeInspiration);
        }

        [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
        [HarmonyPostfix]
        public static void IsolateProducts(List<Thing> ingredients, IBillGiver billGiver, ref IEnumerable<Thing> __result)
        {
            if (__result != null) __result = supply.Wrap(__result, CalculateSupplyQuality(ingredients, billGiver));
        }

        private static int CalculateSupplyQuality(List<Thing> ingredients, IBillGiver billGiver)
        {
            float value = -1;
            int numIng = 0;
            if (ingredients != null && ModSettings_ConfigurableQuality.useMaterialQuality)
            {
                float total = 0;
                foreach (Thing ingredient in ingredients)
                {
                    CompQuality comp = ingredient.TryGetComp<CompQuality>();
                    if (comp == null) continue;
                    total += (int)comp.Quality * ingredient.stackCount;
                    numIng += ingredient.stackCount;
                }
                if (numIng > 0) value = total / numIng;
            }
            if (billGiver is Building_WorkTable table && ModSettings_ConfigurableQuality.useTableQuality)
            {
                int tableValue = (int)(table.TryGetComp<CompQuality>()?.Quality ?? (QualityCategory)ModSettings_ConfigurableQuality.minWorkQuality);
                value = numIng == 0 ? tableValue : value * (1 - ModSettings_ConfigurableQuality.tableFactor) + tableValue * ModSettings_ConfigurableQuality.tableFactor;
            }
            return value < 0 ? -1 : Mathf.Clamp(GenMath.RoundRandom(value), 0, 6);
        }
    }
}
