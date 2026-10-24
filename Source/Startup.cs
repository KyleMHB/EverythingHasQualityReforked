using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace ConfigurableQuality
{
    [StaticConstructorOnStartup]
    public class Startup
    {

        public static bool stuffPatchRan = false;

        static Startup()
        {
            // The extra queue hop follows callbacks queued by other startup constructors.
            // Providers that enqueue further work or cache defs still need runtime verification.
            LongEventHandler.ExecuteWhenFinished(() => LongEventHandler.ExecuteWhenFinished(Initialize));
        }

        private static void Initialize()
        {
            QualityProgression.Initialize();
            QualityProgression.PrepareStartup();
            QualitySettingsState.FreezeActive();
            QualityCatalog.Build();
            RunHarmony();
            Quality_CompPatch.DefPatch();
            QualityDurabilityProduction.Register();
            QualityEnergyEffects.Register();
            QualityConsumableEffects.Register();
            QualityEquipmentCombat.Register();
            QualityLinkedFurniture.Register();
            if (QualityCatalog.Entries.Any(e => QualityCatalog.Assigned(e.Def))) QualityNativeOutput.Register();
        }

        public static void RunHarmony()
        {
            Harmony harmony = new Harmony("kylemhb.configurablequality");
            // Any ingredient can have incompatible stacks, including native/provider quality
            // when this mod's Stuff assignment is disabled.
            MethodInfo mJump = AccessTools.FindIncludingInnerTypes(typeof(Toils_Haul), t => AccessTools.FirstMethod(t, m => m.Name.Contains("JumpIfAlsoCollectingNextTargetInQueue") && m.ReturnType == typeof(void)));
            if (mJump != null && QualityCatalog.Entries.Any(e => QualityCatalog.Assigned(e.Def)))
            {
                harmony.Patch(mJump, null, null, new HarmonyMethod(typeof(Quality_Construction), "CollectNextTarget_Transpiler"));
                stuffPatchRan = true;
            }
            else if (mJump == null)
            {
                Log.Error("QEverything: unable to find haul queue method for stack compatibility patch.");
            }
            if (QualityCatalog.Assigned(ThingDefOf.Silver))
            {
                Type fixes = typeof(TraderFixes);
                harmony.Patch(AccessTools.Method(typeof(ThingWithComps), "PostGeneratedForTrader"), null, new HarmonyMethod(fixes, "TraderSilverFix"));
                harmony.Patch(AccessTools.PropertyGetter(typeof(Tradeable), "IsCurrency"), null, new HarmonyMethod(fixes, "IsCurrencyFix"));
                harmony.Patch(AccessTools.PropertyGetter(typeof(TradeDeal), "CurrencyTradeable"), new HarmonyMethod(fixes, "QualityCurrencyTradeable"));
                harmony.Patch(AccessTools.Method(typeof(Tradeable), "InitPriceDataIfNeeded"), new HarmonyMethod(fixes, "SilverValueFix"));
                harmony.Patch(AccessTools.Method(typeof(TransferableUIUtility), "DefaultListOrderPriority", new Type[] { typeof(ThingDef) }), new HarmonyMethod(fixes, "SortSilver"));
            }
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
