using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace QualityEverything
{
    internal static class QualityPatchOwnership
    {
        // Only these adapters belong to this mod; an unknown suffix is still an external provider.
        private static bool IsInternal(string owner) => owner == "rimworld.qualityframework"
            || owner == "rimworld.qualityframework.energy"
            || owner == "rimworld.qualityframework.consumables"
            || owner == "rimworld.qualityframework.furniture"
            || owner == "rimworld.qualityframework.native-output";

        internal static bool HasExternalPatches(MethodBase method) =>
            Harmony.GetPatchInfo(method)?.Owners.Any(owner => !IsInternal(owner)) == true;
    }
}
