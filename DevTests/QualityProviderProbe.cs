using System.IO;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

// Stands in for another quality mod (Quality Expanded, or Combat Extended's ranged cooldown curve) that already scales
// something Configurable Quality has an effect for. The Harmony ID and class name contain "quality" on purpose: that is how
// the mod recognises a quality provider.
// - StatWorker.StatOffsetFromGear: a postfix that doubles every gear offset, so its presence shows in the numbers.
// - Building_TurretGun.BurstCooldownTime: a no-op postfix.
// - RangedWeapon_Cooldown: a vanilla StatPart_Quality with Legendary x0.5.
// Only the "provider" and "takeover" mod sets enable it, through a marker settings file the launcher copies into the Config folder.
namespace ProviderProbe;

public sealed class QualityProviderProbe : Mod
{
    public const string Owner = "devtests.qualityprovider";
    public const float GearMultiplier = 2f, LegendaryCooldown = .5f;
    public static bool Active { get; private set; }
    public static bool StatPartAdded { get; internal set; }

    public static MethodBase GearMethod => AccessTools.Method(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear));
    public static MethodBase TurretMethod => AccessTools.Method(typeof(Building_TurretGun), "BurstCooldownTime");

    public QualityProviderProbe(ModContentPack content) : base(content)
    {
        string marker = Path.Combine(GenFilePaths.ConfigFolderPath, "Mod_" + content.FolderName + "_QualityProviderProbe.xml");
        if (!File.Exists(marker)) return;
        var harmony = new Harmony(Owner);
        harmony.Patch(GearMethod, postfix: new HarmonyMethod(typeof(QualityProviderProbe), nameof(DoubleGear)));
        harmony.Patch(TurretMethod, postfix: new HarmonyMethod(typeof(QualityProviderProbe), nameof(Noop)));
        Active = true;
        Log.Message("[DevTests] Quality provider probe installed on StatOffsetFromGear and BurstCooldownTime");
    }

    private static void DoubleGear(ref float __result) => __result *= GearMultiplier;
    private static void Noop() { }
}

// Defs are loaded by now, and the mod's own registration is queued behind every startup constructor.
[StaticConstructorOnStartup]
public static class QualityProviderProbeStats
{
    static QualityProviderProbeStats()
    {
        if (!QualityProviderProbe.Active) return;
        var stat = StatDefOf.RangedWeapon_Cooldown;
        var part = new StatPart_Quality { parentStat = stat };
        AccessTools.Field(typeof(StatPart_Quality), "factorLegendary").SetValue(part, QualityProviderProbe.LegendaryCooldown);
        if (stat.parts == null) stat.parts = new System.Collections.Generic.List<StatPart>();
        stat.parts.Add(part);
        QualityProviderProbe.StatPartAdded = true;
    }
}
