using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ConfigurableQuality
{
    // Mechanoids are pawns, not catalog items, so their quality lives in this mod's own comp and their effects bypass the item catalog.
    // The comp is not a CompQuality: vanilla StatPart_Quality on armor and market value would otherwise scale every mech outside these settings.
    internal static class QualityMechanoids
    {
        internal const string QualityMechanoidsId = "Blorp.QualityMechanoids";
        private sealed class MechStat
        {
            public string Name;
            public bool LowerIsBetter;
        }
        private static MechStat Higher(string name) => new MechStat { Name = name };
        private static MechStat Lower(string name) => new MechStat { Name = name, LowerIsBetter = true };
        private static readonly Dictionary<EffectId, MechStat[]> mechStats = new Dictionary<EffectId, MechStat[]>
        {
            { EffectId.MechWork, new[] { Higher("MoveSpeed"), Higher("WorkSpeedGlobal"), Higher("CarryingCapacity"), Higher("MedicalTendQuality") } },
            { EffectId.MechCombat, new[] { Higher("MeleeHitChance"), Higher("MeleeDodgeChance"), Higher("ShootingAccuracyPawn"), Higher("MeleeDamageFactor"),
                Lower("AimingDelayFactor"), Lower("MeleeCooldownFactor"), Lower("RangedCooldownFactor") } },
            { EffectId.MechArmor, new[] { Higher("ArmorRating_Sharp"), Higher("ArmorRating_Blunt"), Higher("ArmorRating_Heat"), Lower("IncomingDamageFactor") } },
            { EffectId.MechEnergy, new[] { Lower("MechEnergyUsageFactor"), Lower("MechEnergyLossPerHP") } }
        };
        internal static readonly EffectId[] Ids = mechStats.Keys.ToArray();
        private static readonly List<StatDef> owned = new List<StatDef>();
        // Settings are fixed at startup, so stat parts exist only for the effects that are on.
        private static readonly HashSet<EffectId> active = new HashSet<EffectId>();
        private static bool registered;

        private static string Text(string suffix) => ("QEverything.Mechanoids." + suffix).Translate();
        private static bool Enabled(EffectId id) => QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(id.ToString(), out var effect) && effect.Enabled;
        // Quality is only rolled while some mech effect is on, so a fresh install leaves gestation and generation untouched.
        internal static bool AnyEnabled => Ids.Any(Enabled);
        // Quality Mechanoids' own part scales the mech by its own quality; it is not a gear-quality provider.
        internal static bool IsMechQualityPart(StatPart part) => part != null && part.GetType().Assembly != typeof(QualityMechanoids).Assembly
            && part.GetType().Name.IndexOf("MechQuality", StringComparison.OrdinalIgnoreCase) >= 0;

        internal static void Register()
        {
            if (registered) return;
            registered = true;
            var races = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.race?.IsMechanoid == true).ToList();
            // Always added, even with every effect off, so a mech's rolled quality survives switching the effects off and on again.
            foreach (var race in races)
            {
                if (race.comps == null) race.comps = new List<CompProperties>();
                if (!race.comps.Any(c => c?.compClass == typeof(CompMechQuality))) race.comps.Add(new CompProperties(typeof(CompMechQuality)));
            }
            var harmony = new Harmony("kylemhb.configurablequality.mechanoids");
            bool hooks = Patch(harmony, AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }), nameof(Generated))
                && Patch(harmony, AccessTools.Method(typeof(Bill_ProductionMech), nameof(Bill_ProductionMech.CreateProducts)), nameof(Gestated));
            // Quality Mechanoids gives mechs a CompQuality, so vanilla quality curves such as those on armor already apply to them.
            bool foreignMod = ModsConfig.IsActive(QualityMechanoidsId);
            foreach (var pair in mechStats)
            {
                var id = pair.Key;
                bool enabled = Enabled(id);
                if (races.Count == 0) { QualityEffects.SetCapability(id, false, Text("NoMechs"), noItems: true); continue; }
                if (!hooks) { QualityEffects.SetCapability(id, false, Text("Conflict")); continue; }
                int found = 0, mine = 0, native = 0;
                bool tookOver = false;
                foreach (var entry in pair.Value)
                {
                    var stat = DefDatabase<StatDef>.GetNamedSilentFail(entry.Name);
                    if (stat == null) continue;
                    found++;
                    // A vanilla curve also scales apparel, so it is never removed; with Quality Mechanoids loaded that stat stays with it.
                    if (foreignMod && stat.parts?.Any(p => p is StatPart_Quality) == true) { native++; continue; }
                    if (stat.parts?.Any(IsMechQualityPart) == true)
                    {
                        if (!QualityEffects.TakesOver(id)) { native++; continue; }
                        stat.parts.RemoveAll(IsMechQualityPart);
                        tookOver = true;
                    }
                    mine++;
                    if (!enabled) continue;
                    if (stat.parts == null) stat.parts = new List<StatPart>();
                    stat.parts.Add(new StatPart_QualityMechanoid(id, entry.LowerIsBetter) { parentStat = stat });
                    if (!owned.Contains(stat)) owned.Add(stat);
                }
                if (enabled && mine > 0) active.Add(id);
                if (tookOver) QualityEffects.MarkTakenOver("Quality Mechanoids", id);
                // Energy stats come from Biotech, so without it that group has nothing to act on.
                if (found == 0) QualityEffects.SetCapability(id, false, Text("NoStats"), noItems: true);
                else if (mine == 0) QualityEffects.SetCapability(id, false, Text("Foreign"), native > 0);
                else QualityEffects.SetCapability(id, true, Text(native > 0 ? "Partial" : "Available"));
            }
        }

        private static bool Patch(Harmony harmony, MethodInfo method, string postfix)
        {
            if (method == null) return false;
            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(QualityMechanoids), postfix));
                return true;
            }
            catch (Exception error)
            {
                Log.Warning("Configurable Quality: mechanoid quality left " + method.Name + " unchanged: " + error.Message);
                return false;
            }
        }

        // Quality Mechanoids' CompQuality wins when present, so both mods agree on what a mech's quality is.
        internal static QualityCategory? QualityOf(Pawn pawn)
        {
            var foreign = pawn.TryGetComp<CompQuality>();
            if (foreign != null) return foreign.Quality;
            var own = pawn.TryGetComp<CompMechQuality>();
            return own != null && own.IsSet ? own.Quality : (QualityCategory?)null;
        }
        internal static float Factor(EffectId id, Thing thing)
        {
            if (!active.Contains(id) || !(thing is Pawn pawn) || pawn.RaceProps?.IsMechanoid != true || !QualityEffects.Available(id)) return 1f;
            var quality = QualityOf(pawn);
            return quality == null ? 1f : QualityEffects.TierFactor(id, (int)quality.Value);
        }

        private static void SetQuality(Pawn mech, QualityCategory quality)
        {
            var comp = mech.TryGetComp<CompMechQuality>();
            if (comp == null) return;
            comp.Quality = quality;
            foreach (var stat in owned) stat.Worker.ClearCacheForThing(mech);
        }
        private static void Generated(Pawn __result)
        {
            if (__result?.RaceProps?.IsMechanoid != true || !AnyEnabled) return;
            var comp = __result.TryGetComp<CompMechQuality>();
            // Raiders, ancient mechs and quest rewards: mostly Normal or Good, rarely the extremes.
            if (comp != null && !comp.IsSet) SetQuality(__result, QualityUtility.GenerateQualityTraderItem());
        }
        // A gestated mech is made by its mechanitor, so the mechanitor's Crafting skill (and a creative inspiration) sets its quality.
        private static void Gestated(Bill_ProductionMech __instance, Thing __result)
        {
            if (!(__result is Pawn mech) || !AnyEnabled) return;
            var maker = __instance.BoundPawn;
            if (maker?.skills == null) return;
            SetQuality(mech, QualityUtility.GenerateQualityCreatedByPawn(maker, SkillDefOf.Crafting, true));
        }
    }

    internal sealed class CompMechQuality : ThingComp
    {
        // -1 means never rolled: mechs from before this mod or from while every mech effect was off stay neutral.
        private int quality = -1;
        public bool IsSet => quality >= 0;
        public QualityCategory Quality
        {
            get => IsSet ? (QualityCategory)quality : QualityCategory.Normal;
            set => quality = (int)value;
        }
        public override void PostExposeData() => Scribe_Values.Look(ref quality, "configurableQuality", -1);
        public override string CompInspectStringExtra()
        {
            if (!IsSet || !QualityMechanoids.AnyEnabled || parent.TryGetComp<CompQuality>() != null) return null;
            return "QEverything.Mechanoids.Inspect".Translate(Quality.GetLabel().CapitalizeFirst());
        }
    }

    internal sealed class StatPart_QualityMechanoid : StatPart
    {
        private readonly EffectId effect;
        private readonly bool lowerIsBetter;
        internal StatPart_QualityMechanoid(EffectId effect, bool lowerIsBetter) { this.effect = effect; this.lowerIsBetter = lowerIsBetter; }
        // The configured factor is "how much better": a lower-is-better stat such as energy use is divided by it.
        private float Factor(StatRequest req)
        {
            float factor = req.HasThing ? QualityMechanoids.Factor(effect, req.Thing) : 1f;
            return factor <= 0f ? 1f : lowerIsBetter ? 1f / factor : factor;
        }
        public override void TransformValue(StatRequest req, ref float val) => val *= Factor(req);
        public override string ExplanationPart(StatRequest req)
        {
            float factor = Factor(req);
            if (factor == 1f || !(req.Thing is Pawn pawn)) return null;
            var quality = QualityMechanoids.QualityOf(pawn);
            return "QEverything.Mechanoids.Explanation".Translate((quality ?? QualityCategory.Normal).GetLabel(), factor.ToStringPercent());
        }
    }
}
