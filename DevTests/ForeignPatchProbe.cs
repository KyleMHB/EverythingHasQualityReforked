using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

// Stands in for an unrelated mod (Performance Fish, a caching mod) that patches the methods Configurable Quality's equipment
// effects hook. Before 2026-11-12 any such patch switched every gear effect off. Mod constructors run before
// [StaticConstructorOnStartup], so these patches are in place when the mod registers its own.
// Only the "equipment" mod set enables it, through a marker settings file the launcher copies into the Config folder.
// The owner ID and this namespace must not contain "quality": the mod treats such patches as a quality provider and steps aside.
namespace ForeignCacheProbe;

public sealed class ForeignPatchProbe : Mod
{
    public const string Owner = "devtests.foreigncache";
    private const string ModOwner = "kylemhb.configurablequality";

    // Each hooked method, and whether Configurable Quality had already patched it when the probe ran.
    public static readonly Dictionary<string, bool> ModOwnedAtProbe = new Dictionary<string, bool>();
    public static bool Active { get; private set; }

    public static IEnumerable<(string name, MethodBase method)> Targets() => new (string, MethodBase)[]
    {
        ("CompQuality.SetQuality", AccessTools.Method(typeof(CompQuality), nameof(CompQuality.SetQuality))),
        ("Thing.Notify_Equipped", AccessTools.Method(typeof(Thing), nameof(Thing.Notify_Equipped))),
        ("Thing.Notify_Unequipped", AccessTools.Method(typeof(Thing), nameof(Thing.Notify_Unequipped))),
        ("StatWorker.StatOffsetFromGear", AccessTools.Method(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear))),
    };

    public ForeignPatchProbe(ModContentPack content) : base(content)
    {
        string marker = Path.Combine(GenFilePaths.ConfigFolderPath, "Mod_" + content.FolderName + "_ForeignPatchProbe.xml");
        if (!File.Exists(marker)) return;
        var harmony = new Harmony(Owner);
        foreach (var (name, method) in Targets())
        {
            ModOwnedAtProbe[name] = Harmony.GetPatchInfo(method)?.Owners.Contains(ModOwner) == true;
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(ForeignPatchProbe), nameof(Noop)));
        }
        Active = true;
        Log.Message("[DevTests] Foreign patch probe installed on " + string.Join(", ", ModOwnedAtProbe.Keys));
    }

    private static void Noop() { }
}
