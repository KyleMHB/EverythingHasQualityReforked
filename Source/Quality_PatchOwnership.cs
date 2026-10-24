using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ConfigurableQuality
{
    internal static class QualityPatchOwnership
    {
        // Only these adapters belong to this mod; an unknown suffix is still an external provider.
        private static bool IsInternal(string owner) => owner == "kylemhb.configurablequality"
            || owner == "kylemhb.configurablequality.energy"
            || owner == "kylemhb.configurablequality.consumables"
            || owner == "kylemhb.configurablequality.furniture"
            || owner == "kylemhb.configurablequality.native-output";

        internal static bool HasExternalPatches(MethodBase method) =>
            Harmony.GetPatchInfo(method)?.Owners.Any(owner => !IsInternal(owner)) == true;
    }
}
