using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;

namespace QualityEverything
{
    public class TraderFixes
    {
        private static readonly FieldInfo BuyPrice = AccessTools.Field(typeof(Tradeable), "pricePlayerBuy");
        private static readonly FieldInfo SellPrice = AccessTools.Field(typeof(Tradeable), "pricePlayerSell");

        //Traders only carry normal silver
        public static void TraderSilverFix(ThingWithComps __instance)
        {
            if (__instance.def == ThingDefOf.Silver && QualityCatalog.Assigned(__instance.def))
            {
                CompQuality comp = __instance.GetComp<CompQuality>();
                if (comp != null) QualityNativeOutput.SetNativeQuality(comp, QualityCategory.Normal, ArtGenerationContext.Outsider);
            }
        }

        //Only normal silver is currency
        public static void IsCurrencyFix(ref bool __result, Tradeable __instance)
        {
            if (__result && __instance.ThingDef == ThingDefOf.Silver && __instance.AnyThing?.TryGetComp<CompQuality>() != null)
            {
                QualityCategory qc;
                __instance.AnyThing.TryGetQuality(out qc);
                __result = qc == QualityCategory.Normal;
            }
        }

        //Tradeable currency for a session can only be favor or normal quality silver
        public static bool QualityCurrencyTradeable(ref Tradeable __result, TradeDeal __instance, List<Tradeable> ___tradeables)
        {
            if (TradeSession.TradeCurrency != TradeCurrency.Silver || !QualityCatalog.Assigned(ThingDefOf.Silver)) return true;
            for (int i = 0; i < ___tradeables.Count; i++)
            {
                Tradeable tradeable = ___tradeables[i];
                if (!tradeable.IsFavor && tradeable.ThingDef == ThingDefOf.Silver)
                {
                    QualityCategory qc;
                    if (tradeable.AnyThing == null)
                    {
                        continue;
                    }

                    if (!tradeable.AnyThing.TryGetQuality(out qc) || qc == QualityCategory.Normal)
                    {
                        __result = tradeable;
                        return false;
                    }
                }
            }
            __result = null;
            return false;
        }

        //Silver price is the same for buying or selling; allows conversion of silver to different quality
        public static bool SilverValueFix(Tradeable __instance)
        {
            if (__instance.ThingDef == ThingDefOf.Silver && __instance.AnyThing?.TryGetComp<CompQuality>() != null && BuyPrice != null && SellPrice != null)
            {
                BuyPrice.SetValue(__instance, __instance.BaseMarketValue);
                SellPrice.SetValue(__instance, __instance.BaseMarketValue);
                return false;
            }
            return true;
        }

        public static bool SortSilver(ref float __result, ThingDef def)
        {
            if (def == ThingDefOf.Silver && QualityCatalog.Assigned(def))
            {
                __result = -20f;
                return false;
            }

            return true;
        }
    }
}
