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

        // A foreign patch whose Harmony ID or patch class names quality (Quality Expanded: "rimworld.qualityexpanded")
        // is taken as that mod already scaling the result by quality. Unrelated patches on busy methods are ignored.
        internal static bool HasExternalQualityPatches(MethodBase method) => ExternalQualityOwners(method).Any();

        // Harmony IDs of the foreign quality providers, so the settings screen can name the mod that blocks an effect.
        internal static string[] ExternalQualityOwners(MethodBase method)
        {
            return ExternalQualityPatches(method).Select(p => p.owner).Distinct().ToArray();
        }

        // Take-over removes only the foreign patches that name quality, so unrelated patches on the same method keep working.
        // Returns the Harmony IDs whose patches were removed.
        internal static string[] RemoveExternalQualityPatches(MethodBase method)
        {
            var patches = ExternalQualityPatches(method);
            var remover = new Harmony("kylemhb.configurablequality");
            foreach (var patch in patches) remover.Unpatch(method, patch.PatchMethod);
            return patches.Select(p => p.owner).Distinct().ToArray();
        }

        private static Patch[] ExternalQualityPatches(MethodBase method)
        {
            var info = method == null ? null : Harmony.GetPatchInfo(method);
            if (info == null) return new Patch[0];
            return info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers)
                .Where(p => !IsInternal(p.owner) && (Names(p.owner) || Names(p.PatchMethod?.DeclaringType?.FullName))).ToArray();
        }
        private static bool Names(string text) => text != null && text.IndexOf("quality", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
