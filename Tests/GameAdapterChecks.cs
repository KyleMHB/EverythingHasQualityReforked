using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using System.Collections;
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
    private static MethodInfo Adapter(string type, string method) => AccessTools.Method(mod.GetType("ConfigurableQuality." + type, true), method)
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
        var production = mod.GetType("ConfigurableQuality.ProductionSettings", true);
        var defaults = Activator.CreateInstance(production);
        var compatibility = mod.GetType("ConfigurableQuality.ModSettings_ConfigurableQuality", true);
        foreach (var field in production.GetFields())
            if (!Equals(field.GetValue(defaults), compatibility.GetField(field.Name).GetValue(null)))
                throw new Exception("Compatibility default differs: " + field.Name);
        Console.WriteLine("Matched all public scalar defaults to ProductionSettings.");
        if ((int)Adapter("Quality_Construction", "CalculateMaterialQuality").Invoke(null, new object[] { null }) != -1) throw new Exception("Passive construction must bypass material calculations and random rolls.");
        CheckSettingsImports();
        CheckMarketValue();
        CheckVanillaRemoval();
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

    private static void CheckVanillaRemoval()
    {
        var defType = game.GetType("Verse.ThingDef", true);
        var propertyType = game.GetType("Verse.CompProperties", true);
        var qualityType = game.GetType("RimWorld.CompQuality", true);
        object def = RuntimeHelpers.GetUninitializedObject(defType);
        var contentType = game.GetType("Verse.ModContentPack", true);
        object content = RuntimeHelpers.GetUninitializedObject(contentType);
        AccessTools.Field(contentType, "official").SetValue(content, true);
        AccessTools.Field(defType, "modContentPack").SetValue(def, content);
        var properties = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(propertyType));
        object unrelated = Activator.CreateInstance(propertyType), quality = Activator.CreateInstance(propertyType);
        AccessTools.Field(propertyType, "compClass").SetValue(unrelated, game.GetType("RimWorld.CompArt", true));
        AccessTools.Field(propertyType, "compClass").SetValue(quality, qualityType);
        properties.Add(unrelated); properties.Add(quality);
        AccessTools.Field(defType, "comps").SetValue(def, properties);
        var catalog = mod.GetType("ConfigurableQuality.QualityCatalog", true);
        var canRemove = AccessTools.Method(catalog, "CanRemoveVanillaQuality");
        bool Eligible(bool supported = true, bool native = true) => (bool)canRemove.Invoke(null, new[] { def, (object)supported, native });
        void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
        Expect(Eligible(), "Standard vanilla quality must be eligible for explicit removal.");
        Expect(!Eligible(false) && !Eligible(true, false), "Unsupported and non-native defs must not be eligible.");
        AccessTools.Field(contentType, "official").SetValue(content, false);
        Expect(!Eligible(), "Other-mod quality must remain protected.");
        AccessTools.Field(contentType, "official").SetValue(content, true);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("RemovalCustomQualityCheck"), AssemblyBuilderAccess.Run);
        var customQuality = assembly.DefineDynamicModule("main").DefineType("CustomQuality", TypeAttributes.Public, qualityType).CreateType();
        AccessTools.Field(propertyType, "compClass").SetValue(quality, customQuality);
        Expect(!Eligible(), "Custom quality subclasses on vanilla defs must remain protected.");
        AccessTools.Field(propertyType, "compClass").SetValue(quality, qualityType);
        var removals = AccessTools.Field(catalog, "activeVanillaRemoval").GetValue(null);
        var removalType = removals.GetType();
        var assignments = (IDictionary)AccessTools.Field(catalog, "activeAssignment").GetValue(null);
        var resolutionType = mod.GetType("ConfigurableQuality.RuleResolution", true);
        assignments[def] = Activator.CreateInstance(resolutionType, false, "VanillaRemoval", "Check", false);
        var sync = AccessTools.Method(mod.GetType("ConfigurableQuality.QualityCompSync", true), "SyncDef");
        try
        {
            removalType.GetMethod("Add").Invoke(removals, new[] { def });
            sync.Invoke(null, new[] { def });
            sync.Invoke(null, new[] { def });
            Expect(properties.Count == 1 && ReferenceEquals(properties[0], unrelated), "Repeated removal must preserve unrelated properties and never re-add quality.");
            removalType.GetMethod("Remove").Invoke(removals, new[] { def });
            sync.Invoke(null, new[] { def });
            Expect(properties.Count == 2 && ReferenceEquals(properties[0], unrelated) && ReferenceEquals(properties[1], quality), "Removing an opt-in must restore original properties by reference and position.");
            sync.Invoke(null, new[] { def });
            Expect(properties.Count == 2, "Repeated restoration must not duplicate native properties.");
            Console.WriteLine("Vanilla quality removal eligibility, provider protection, repeated synchronization and restoration passed.");
        }
        finally
        {
            removalType.GetMethod("Remove").Invoke(removals, new[] { def });
            assignments.Remove(def);
        }
    }

    private static void CheckSettingsImports()
    {
        var configurationType = mod.GetType("ConfigurableQuality.QualityConfiguration", true);
        var sessionType = mod.GetType("ConfigurableQuality.QualityConfigurationSession", true);
        var stateType = mod.GetType("ConfigurableQuality.QualitySettingsState", true);
        var sessionField = AccessTools.Field(stateType, "session");
        var frozenField = AccessTools.Field(stateType, "runtimeInitialized");
        object oldSession = sessionField.GetValue(null), oldFrozen = frozenField.GetValue(null);
        object configuration = Activator.CreateInstance(configurationType);
        object production = AccessTools.Field(configurationType, "Production").GetValue(configuration);
        var tableFactor = AccessTools.Field(production.GetType(), "tableFactor");
        try
        {
            sessionField.SetValue(null, Activator.CreateInstance(sessionType, configuration));
            frozenField.SetValue(null, false);
            tableFactor.SetValue(production, .7f);
            AccessTools.Method(stateType, "Load").Invoke(null, new[] { configuration });
            object Snapshot(string name) => AccessTools.Property(stateType, name).GetValue(null);
            float Factor(string name) => (float)tableFactor.GetValue(AccessTools.Field(configurationType, "Production").GetValue(Snapshot(name)));
            if (Factor("ActiveSnapshot") != .7f) throw new Exception("Startup preset import must activate before defs are built.");
            // Also cover a fresh installation: startup can finish without any settings file loading.
            sessionField.SetValue(null, Activator.CreateInstance(sessionType, Activator.CreateInstance(configurationType)));
            AccessTools.Method(stateType, "FreezeActive").Invoke(null, null);
            AccessTools.Method(stateType, "Load").Invoke(null, new[] { configuration });
            if (Factor("ActiveSnapshot") != .4f || Factor("Saved") != .7f || Factor("Draft") != .7f)
                throw new Exception("Runtime config imports must wait for restart, including on fresh installs.");
            if (!(bool)AccessTools.Property(stateType, "RestartPending").GetValue(null)) throw new Exception("Imported settings must report restart pending.");
            Console.WriteLine("Startup preset loading and runtime settings import isolation passed, including fresh installations.");
        }
        finally
        {
            sessionField.SetValue(null, oldSession);
            frozenField.SetValue(null, oldFrozen);
            AccessTools.Method(stateType, "Changed").Invoke(null, null);
            // Load deliberately refreshes the retained public fields from the active snapshot.
            var defaults = Activator.CreateInstance(mod.GetType("ConfigurableQuality.ProductionSettings", true));
            var compatibility = mod.GetType("ConfigurableQuality.ModSettings_ConfigurableQuality", true);
            foreach (var field in defaults.GetType().GetFields()) compatibility.GetField(field.Name).SetValue(null, field.GetValue(defaults));
        }
    }

    private static void CheckMarketValue()
    {
        var configurationType = mod.GetType("ConfigurableQuality.QualityConfiguration", true);
        var sessionType = mod.GetType("ConfigurableQuality.QualityConfigurationSession", true);
        var state = mod.GetType("ConfigurableQuality.QualitySettingsState", true);
        var sessionField = AccessTools.Field(state, "session");
        object previousSession = sessionField.GetValue(null);
        var catalog = mod.GetType("ConfigurableQuality.QualityCatalog", true);
        var byDef = (IDictionary)AccessTools.Field(catalog, "byDef").GetValue(null);
        var assigned = (IDictionary)AccessTools.Field(catalog, "activeAssignment").GetValue(null);
        var marketValues = (IDictionary)AccessTools.Field(catalog, "activeMarketValue").GetValue(null);
        var defType = game.GetType("Verse.ThingDef", true);
        object def = RuntimeHelpers.GetUninitializedObject(defType);
        AccessTools.Field(defType, "defName").SetValue(def, "MarketValueCheckWall");
        var categoryType = mod.GetType("ConfigurableQuality.CategoryPath", true);
        var categories = Array.CreateInstance(categoryType, 1);
        categories.SetValue(Activator.CreateInstance(categoryType, "Walls", 1), 0);
        var target = Activator.CreateInstance(mod.GetType("ConfigurableQuality.RuleTarget", true), "MarketValueCheckWall", "Building", true, categories);
        var entryType = mod.GetType("ConfigurableQuality.QualityCatalogEntry", true);
        var entry = Activator.CreateInstance(entryType, true);
        AccessTools.Field(entryType, "Def").SetValue(entry, def);
        AccessTools.Field(entryType, "Target").SetValue(entry, target);
        var resolutionType = mod.GetType("ConfigurableQuality.RuleResolution", true);
        object Enabled(bool yes) => Activator.CreateInstance(resolutionType, yes, "Group", "Building", false);
        var statType = game.GetType("RimWorld.StatDef", true);
        object stat = RuntimeHelpers.GetUninitializedObject(statType);
        AccessTools.Field(statType, "defName").SetValue(stat, "MarketValue");
        var partType = game.GetType("RimWorld.StatPart_Quality", true);
        object part = Activator.CreateInstance(partType);
        AccessTools.Field(partType, "parentStat").SetValue(part, stat);
        var requestType = game.GetType("RimWorld.StatRequest", true);
        var qualityType = game.GetType("RimWorld.QualityCategory", true);
        var forDef = AccessTools.Method(requestType, "For", new[] { game.GetType("Verse.BuildableDef", true), defType, qualityType });
        var valuePrefix = AccessTools.Method(mod.GetType("ConfigurableQuality.QualityMarketValue+ValuePatch", true), "Prefix");
        var explanationPrefix = AccessTools.Method(mod.GetType("ConfigurableQuality.QualityMarketValue+ExplanationPatch", true), "Prefix");
        // Check binding against the real game methods as well as invoking the real mod prefixes.
        foreach (string method in new[] { "TransformValue", "ExplanationPart" })
            if (AccessTools.Method(partType, method).GetParameters()[0].ParameterType != requestType) throw new Exception("Market-value patch request signature differs.");
        bool Runs(object req) => (bool)valuePrefix.Invoke(null, new[] { part, req });
        void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }
        try
        {
            byDef.Add(def, entry);
            assigned.Add(def, Enabled(true));
            object configuration = Activator.CreateInstance(configurationType);
            var marketRules = AccessTools.Field(configurationType, "MarketValue").GetValue(configuration);
            var categoryRules = (IDictionary)AccessTools.Field(marketRules.GetType(), "Categories").GetValue(marketRules);
            void Activate()
            {
                sessionField.SetValue(null, Activator.CreateInstance(sessionType, configuration));
                marketValues[def] = AccessTools.Method(mod.GetType("ConfigurableQuality.QualityRules", true), "Resolve").Invoke(null, new[] { target, marketRules });
            }
            Activate();
            object normal = forDef.Invoke(null, new[] { def, null, Enum.ToObject(qualityType, 2) });
            Expect(Runs(normal), "Default pricing must keep native quality scaling.");
            categoryRules.Add("Walls", Enum.Parse(mod.GetType("ConfigurableQuality.RuleChoice", true), "Exclude"));
            Activate();
            foreach (string marketStat in new[] { "MarketValue", "MarketValueIgnoreHp" })
            for (int tier = 0; tier < 7; tier++)
            {
                AccessTools.Field(statType, "defName").SetValue(stat, marketStat);
                object request = forDef.Invoke(null, new[] { def, null, Enum.ToObject(qualityType, tier) });
                Expect(!Runs(request), "Excluded category must suppress price scaling for every quality tier.");
                object[] explanationArgs = { part, request, "old quality multiplier" };
                Expect(!(bool)explanationPrefix.Invoke(null, explanationArgs) && explanationArgs[2] == null, "Disabled price scaling must hide its native explanation.");
                AccessTools.Field(entryType, "NativeQuality").SetValue(entry, true);
                Expect(Runs(request), "Other-mod prices and building wealth must remain protected for all tiers.");
                AccessTools.Field(entryType, "RemovableVanillaQuality").SetValue(entry, true);
                Expect(!Runs(request), "Eligible vanilla pricing follows category rules for every quality tier.");
                AccessTools.Field(entryType, "RemovableVanillaQuality").SetValue(entry, false);
                AccessTools.Field(entryType, "NativeQuality").SetValue(entry, false);
            }
            // Vanilla quality is independently controllable even without added-quality assignment.
            categoryRules.Clear();
            assigned[def] = Enabled(false);
            AccessTools.Field(entryType, "NativeQuality").SetValue(entry, true);
            AccessTools.Field(entryType, "RemovableVanillaQuality").SetValue(entry, true);
            AccessTools.Field(marketRules.GetType(), "Default").SetValue(marketRules, false);
            Activate();
            Expect(!Runs(normal), "Global rule suppresses vanilla pricing without assignment.");
            var priceGroups = (IDictionary)AccessTools.Field(marketRules.GetType(), "Groups").GetValue(marketRules);
            priceGroups.Add("Building", true); Activate();
            Expect(Runs(normal), "Group override restores vanilla price scaling above global default.");
            categoryRules.Add("Walls", Enum.Parse(mod.GetType("ConfigurableQuality.RuleChoice", true), "Exclude")); Activate();
            Expect(!Runs(normal), "Category overrides group for native pricing.");
            var priceItems = (IDictionary)AccessTools.Field(marketRules.GetType(), "Items").GetValue(marketRules);
            priceItems.Add("MarketValueCheckWall", Enum.Parse(mod.GetType("ConfigurableQuality.RuleChoice", true), "Include")); Activate();
            Expect(Runs(normal), "Item override restores native pricing above category.");
            priceItems.Clear(); priceGroups.Clear(); Activate();
            AccessTools.Field(entryType, "NativeQuality").SetValue(entry, false);
            AccessTools.Field(entryType, "RemovableVanillaQuality").SetValue(entry, false);
            assigned[def] = Enabled(true);
            Expect(PatchProcessor.GetOriginalInstructions(Native("RimWorld.WealthWatcher", "ForceRecount"))
                .Any(i => i.operand is FieldInfo field && field.Name == "MarketValueIgnoreHp"), "Native building wealth route changed; review both market-value stats.");
            var thingType = game.GetType("Verse.ThingWithComps", true);
            object thing = RuntimeHelpers.GetUninitializedObject(thingType);
            AccessTools.Field(thingType, "def").SetValue(thing, def);
            object thingRequest = AccessTools.Method(requestType, "For", new[] { game.GetType("Verse.Thing", true) }).Invoke(null, new[] { thing });
            Expect(!Runs(thingRequest), "Actual item requests must use the category price exclusion.");
            AccessTools.Field(entryType, "NativeQuality").SetValue(entry, true);
            Expect(Runs(normal) && Runs(thingRequest), "Native and other-mod quality prices must remain untouched.");
            AccessTools.Field(entryType, "NativeQuality").SetValue(entry, false);
            assigned[def] = Enabled(false);
            Expect(Runs(normal), "Unassigned definitions must remain untouched.");
            assigned[def] = Enabled(true);
            AccessTools.Field(statType, "defName").SetValue(stat, "Beauty");
            Expect(Runs(normal), "Non-market-value quality effects must remain untouched.");
            AccessTools.Field(statType, "defName").SetValue(stat, "MarketValue");
            byDef.Remove(def);
            Expect(Runs(normal), "Unknown definitions must remain untouched.");
            Expect(Runs(AccessTools.Method(requestType, "ForEmpty").Invoke(null, null)), "Empty stat requests must remain untouched.");
            Console.WriteLine("Market-value and building-wealth category routing, all quality tiers, item requests, explanations and native protection passed.");
        }
        finally
        {
            byDef.Remove(def);
            assigned.Remove(def);
            marketValues.Remove(def);
            sessionField.SetValue(null, previousSession);
        }
    }
}
