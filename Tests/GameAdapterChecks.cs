using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using HarmonyLib;

// Reads installed game IL and runs the real mod transpilers, without starting the game
// or installing patches into a game process. This does not certify gameplay/save behavior.
internal static class GameAdapterChecks
{
    private static Assembly game, mod;
    private static int checks;
    private static int Main(string[] args)
    {
        try
        {
            string managedPath = Path.GetFullPath(args[0]);
            string modPath = Path.GetFullPath(args[1]);
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                string path = Path.Combine(managedPath, name.Name + ".dll");
                return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
            };
            game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(managedPath, "Assembly-CSharp.dll"));
            mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(modPath);
            Run();
            Console.WriteLine($"Game adapter checks passed: {checks} native IL transformations.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
    private static MethodInfo Native(string type, string method, params string[] parameters) => AccessTools.Method(game.GetType(type, true), method,
        parameters.Length == 0 ? null : parameters.Select(p => Type.GetType(p) ?? game.GetType(p, true)).ToArray())
        ?? throw new Exception("Missing native method: " + type + "." + method);
    private static MethodInfo Adapter(string type, string method) => AccessTools.Method(mod.GetType("QualityEverything." + type, true), method)
        ?? throw new Exception("Missing adapter: " + type + "." + method);

    private static void Transform(MethodInfo target, string owner, string transpiler, string[] helpers, int expectedCalls)
    {
        var original = PatchProcessor.GetOriginalInstructions(target, out ILGenerator generator);
        var transform = Adapter(owner, transpiler);
        object[] arguments = transform.GetParameters().Select(p => p.ParameterType == typeof(ILGenerator) ? (object)generator
            : p.ParameterType == typeof(MethodBase) ? target : original).ToArray();
        var transformed = ((IEnumerable<CodeInstruction>)transform.Invoke(null, arguments)).ToList();
        int calls = transformed.Count(i => i.operand is MethodInfo method && method.DeclaringType?.Assembly == mod && helpers.Contains(method.Name));
        if (calls != expectedCalls) throw new Exception($"{target}: expected {expectedCalls} adapter calls, got {calls}.");
        checks++;
        Console.WriteLine("Matched " + target.DeclaringType.FullName + "." + target.Name);
    }
    private static void Run()
    {
        // Access the real compatibility fields to verify their static initialization,
        // without reading or writing a player's saved settings.
        var production = mod.GetType("QualityEverything.ProductionSettings", true);
        var defaults = Activator.CreateInstance(production);
        var compatibility = mod.GetType("QualityEverything.ModSettings_QEverything", true);
        foreach (var field in production.GetFields())
            if (!Equals(field.GetValue(defaults), compatibility.GetField(field.Name).GetValue(null)))
                throw new Exception("Compatibility default differs: " + field.Name);
        Console.WriteLine("Matched all public scalar defaults to ProductionSettings.");
        Transform(Native("RimWorld.Frame", "CompleteConstruction"), "Quality_Construction", "ConstructionQuality", new[] { "GenerateConstructionQuality" }, 1);
        Transform(Native("Verse.GenRecipe", "PostProcessProduct"), "Quality_Recipes", "RecipeQuality", new[] { "GenerateRecipeQuality", "ReportMissingSkill" }, 2);
        Transform(Native("RimWorld.Mineable", "TrySpawnYield", "Verse.Map", "System.Boolean", "Verse.Pawn"), "Quality_Resources", "MiningQuality", new[] { "GenerateResourceQuality" }, 1);
        Transform(Native("RimWorld.CompHasGatherableBodyResource", "Gathered"), "Quality_Resources", "AnimalQuality", new[] { "GenerateResourceQuality" }, 1);
        Transform(Native("RimWorld.CompEggLayer", "ProduceEgg"), "Quality_Resources", "EggQuality", new[] { "GenerateResourceQuality" }, 2);
        Transform(Native("RimWorld.TendUtility", "DoTend"), "QualityMedicineEffects", "TendTranspiler", new[] { "Tend" }, 1);
        Transform(Native("RimWorld.Bill_Medical", "Notify_BillWorkStarted"), "QualityMedicineEffects", "InitialMedicineTranspiler", new[] { "ConsumeInitial" }, 1);
        Transform(Native("RimWorld.IngestionOutcomeDoer_GiveHediff", "DoIngestionOutcomeSpecial"), "QualityDrugEffects", "GiveTranspiler", new[] { "AddDose" }, 1);
        Transform(Native("RimWorld.IngestionOutcomeDoer_OffsetNeed", "DoIngestionOutcomeSpecial"), "QualityDrugEffects", "NeedTranspiler", new[] { "SetNeed" }, 1);
        Transform(Native("RimWorld.IngestionOutcomeDoer_OffsetPsyfocus", "DoIngestionOutcomeSpecial"), "QualityDrugEffects", "PsyfocusTranspiler", new[] { "Psyfocus" }, 1);
        Transform(Native("RimWorld.CompPowerTrader", "CompInspectStringExtra"), "QualityEnergyEffects", "PowerInspect", new[] { "ActiveDemand" }, 1);
        foreach (string name in new[] { "get_AmountCanAccept", "get_StoredEnergyPct", "SetStoredEnergyPct", "AddEnergy", "PostExposeData", "CompInspectStringExtra" })
        {
            int expected = name == "get_AmountCanAccept" || name == "PostExposeData" || name == "CompInspectStringExtra" ? 2 : 1;
            Transform(Native("RimWorld.CompPowerBattery", name), "QualityEnergyEffects", "BatteryFields", new[] { "Maximum", "EffectiveMaximum", "Efficiency" }, expected);
        }
        Transform(Native("RimWorld.Building_Battery", "DrawAt"), "QualityEnergyEffects", "BatteryFields", new[] { "BuildingMaximum" }, 1);
        var haulType = game.GetType("Verse.AI.Toils_Haul", true);
        var haul = AccessTools.FindIncludingInnerTypes(haulType, t => AccessTools.FirstMethod(t,
            m => m.Name.Contains("JumpIfAlsoCollectingNextTargetInQueue") && m.ReturnType == typeof(void)))
            ?? throw new Exception("Missing native haul queue callback.");
        var before = PatchProcessor.GetOriginalInstructions(haul);
        var after = ((IEnumerable<CodeInstruction>)Adapter("Quality_Construction", "CollectNextTarget_Transpiler").Invoke(null, new object[] { before })).ToList();
        if (after.Count(i => i.operand is MethodInfo m && m.Name == "CanStackWith") != 1)
            throw new Exception("Haul queue does not check stack compatibility.");
        checks++;
        Console.WriteLine("Matched native haul queue stack compatibility.");
    }
}
