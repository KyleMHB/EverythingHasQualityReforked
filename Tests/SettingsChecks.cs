using System;
using System.Linq;
using ConfigurableQuality;
class SettingsChecks
{
    static void Check(bool actual, string name) { if (!actual) throw new Exception(name); }
    static void Main()
    {
        var rules = new RuleSet();
        rules.Groups["Stuff"] = true;
        rules.Categories["Resources"] = RuleChoice.Include;
        rules.Categories["Textiles"] = RuleChoice.Exclude;
        var cloth = new RuleTarget("Cloth", "Stuff", true, new[] { new CategoryPath("Resources", 1), new CategoryPath("Textiles", 2) });
        Check(!QualityRules.Resolve(cloth, rules).Enabled, "descendant branch overrides group");
        rules.Items["Cloth"] = RuleChoice.Include;
        Check(QualityRules.Resolve(cloth, rules).Enabled, "item overrides branch");
        Check(!QualityRules.Resolve(new RuleTarget("Pawn", "Stuff", false, cloth.Categories), rules).Enabled, "unsupported cannot be forced");
        rules.Items.Clear();
        rules.Categories["AInclude"] = RuleChoice.Include;
        rules.Categories["ZExclude"] = RuleChoice.Exclude;
        foreach (var paths in new[] {
            new[] { new CategoryPath("AInclude", 3), new CategoryPath("ZExclude", 3) },
            new[] { new CategoryPath("ZExclude", 3), new CategoryPath("AInclude", 3) } })
        {
            var resolution = QualityRules.Resolve(new RuleTarget("Multi", "Stuff", true, paths), rules);
            Check(!resolution.Enabled && resolution.Conflict && resolution.SourceId == "ZExclude", "exclude tie and truthful reason independent of order");
        }
        var defaults = new QualityConfiguration();
        var passiveTarget = new RuleTarget("Passive", "Building", true, new CategoryPath[0]);
        Check(!QualityRules.Resolve(passiveTarget, defaults.Assignment).Enabled
            && !QualityRules.Resolve(passiveTarget, defaults.VanillaRemoval).Enabled
            && QualityRules.Resolve(passiveTarget, defaults.MarketValue).Enabled
            && typeof(ProductionSettings).GetFields().Where(f => f.FieldType == typeof(bool)).All(f => !(bool)f.GetValue(defaults.Production)), "fresh install has no gameplay-changing options enabled");
        Check(!QualityRules.Resolve(passiveTarget, QualityConfiguration.MigrateLegacyXml("<ModSettings />").Assignment).Enabled, "empty config does not activate legacy defaults");
        var globalRules = defaults.Clone();
        globalRules.Assignment.Default = true;
        Check(QualityRules.Resolve(passiveTarget, globalRules.Assignment).Enabled, "global assignment fallback applies");
        globalRules.Assignment.Groups["Building"] = false;
        Check(!QualityRules.Resolve(passiveTarget, globalRules.Assignment).Enabled, "group overrides global");
        globalRules.VanillaRemoval.Default = true;
        globalRules.MarketValue.Default = false;
        var globalReload = QualityConfiguration.Parse(globalRules.Serialize());
        Check(globalReload.Assignment.Default && globalReload.VanillaRemoval.Default && !globalReload.MarketValue.Default
            && !globalReload.Assignment.Groups["Building"], "global rules and overrides roundtrip independently");
        var preset = defaults.Clone(); preset.EnablePresetProduction(); preset.ProgressionCompatibility = true;
        Check(QualityConfiguration.Parse(preset.Serialize()).ProgressionCompatibility, "Progression provider-policy marker survives config-manager export/import");
        Check(QualityRules.Resolve(passiveTarget, preset.Assignment).Enabled && preset.Production.useMaterialQuality
            && preset.MarketValue.Default && !preset.VanillaRemoval.Default, "preset activates assignment and production without changing pricing or removal");
        Check(defaults.VanillaRemoval.Groups.Values.All(v => !v) && QualityConfiguration.Parse("<quality />").VanillaRemoval.Groups.Values.All(v => !v),
            "fresh and existing configurations never remove vanilla quality by default");
        var removalDraft = defaults.Clone();
        removalDraft.VanillaRemoval.Categories["Weapons"] = RuleChoice.Include;
        removalDraft.VanillaRemoval.Items["KeepRifle"] = RuleChoice.Exclude;
        var removalRoundtrip = QualityConfiguration.Parse(removalDraft.Serialize());
        Check(removalRoundtrip.VanillaRemoval.Categories["Weapons"] == RuleChoice.Include
            && removalRoundtrip.VanillaRemoval.Items["KeepRifle"] == RuleChoice.Exclude
            && defaults.VanillaRemoval.Categories.Count == 0, "vanilla removal rules persist independently and clone without sharing");
        Check(defaults.MarketValue.Default && defaults.MarketValue.Groups.Values.All(v => v) && QualityConfiguration.Parse("<quality />").MarketValue.Groups.Values.All(v => v),
            "new and existing settings preserve quality pricing by default");
        var valueSession = new QualityConfigurationSession(defaults);
        valueSession.Draft.MarketValue.Categories["Buildings"] = RuleChoice.Exclude;
        Check(valueSession.Saved.MarketValue.Categories.Count == 0 && valueSession.Active.MarketValue.Categories.Count == 0,
            "market-value draft does not change saved or active settings");
        Check(valueSession.Save().Count == 0 && valueSession.Saved.MarketValue.Categories["Buildings"] == RuleChoice.Exclude && valueSession.Active.MarketValue.Categories.Count == 0,
            "market-value switch activates only after restart");
        var reloadedValue = QualityConfiguration.Parse(valueSession.Saved.Serialize());
        Check(reloadedValue.MarketValue.Categories["Buildings"] == RuleChoice.Exclude && reloadedValue.Clone().MarketValue.Categories["Buildings"] == RuleChoice.Exclude,
            "market-value category rule survives save, reload and clone");
        reloadedValue.Assignment.Groups["Building"] = true;
        var wall = new RuleTarget("Wall", "Building", true, new[] { new CategoryPath("Buildings", 1), new CategoryPath("Walls", 2) });
        Check(!QualityRules.Resolve(wall, reloadedValue.MarketValue).Enabled && QualityRules.Resolve(wall, reloadedValue.Assignment).Enabled,
            "market-value category exclusions do not remove quality assignment");
        reloadedValue.MarketValue.Categories["Walls"] = RuleChoice.Include;
        Check(QualityRules.Resolve(wall, reloadedValue.MarketValue).Enabled, "deeper market-value category overrides its parent");
        reloadedValue.MarketValue.Items["Wall"] = RuleChoice.Exclude;
        Check(!QualityRules.Resolve(wall, reloadedValue.MarketValue).Enabled, "market-value item exception overrides category");
        var importedValue = new QualityConfigurationSession(defaults);
        importedValue.Import(reloadedValue);
        Check(importedValue.Active.MarketValue.Categories.Count == 0 && importedValue.Saved.MarketValue.Categories.Count == 2
            && importedValue.Draft.MarketValue.Items["Wall"] == RuleChoice.Exclude,
            "runtime settings import updates saved and draft without changing active pricing");
        reloadedValue.MarketValue.Items.Clear();
        Check(importedValue.Saved.MarketValue.Items.ContainsKey("Wall"), "imported settings are independent snapshots");
        var sharedXml = new System.Xml.Linq.XElement("ModSettings", new System.Xml.Linq.XAttribute("Class", "ConfigurableQuality.ModSettings_ConfigurableQuality"),
            new System.Xml.Linq.XElement("qualityConfiguration", importedValue.Saved.Serialize())).ToString();
        var sharedConfiguration = QualityConfiguration.Parse(System.Xml.Linq.XElement.Parse(sharedXml).Element("qualityConfiguration").Value);
        Check(sharedConfiguration.Serialize() == importedValue.Saved.Serialize(), "standard mod-settings XML preserves complete config for sharing and preset imports");
        foreach (string field in new[] { "tableFactor", "awfulSupplyFactor", "legSupplyFactor" })
            foreach (float value in new[] { -1f, 0f, 1f, 10f, 10.01f, 11f, float.NaN, float.PositiveInfinity })
            {
                var draft = new QualityConfiguration();
                typeof(ProductionSettings).GetField(field).SetValue(draft.Production, value);
                bool inputValid = QualityNumericRange.ForInput("production:" + field).Contains(value);
                Check(inputValid == !draft.Validate().Contains(field), "inline and Save agree for " + field + " = " + value);
            }
        Check(!QualityNumericRange.ForInput("production:awfulSupplyFactor").TryParse("", out _)
            && !QualityNumericRange.Effect.TryParse("-", out _)
            && !QualityNumericRange.Effect.TryParse("NaN", out _)
            && QualityNumericRange.Effect.TryParse("0.01", out _), "partial and invalid input stays outside the draft");
        var nativeWeapon = new RuleTarget("NativeGun", "Weapon", true, new CategoryPath[0]);
        Check(!QualityRules.Resolve(nativeWeapon, defaults.Assignment).Enabled && QualityRules.Resolve(nativeWeapon, defaults.Participation).Enabled,
            "native-quality effects participation is independent of assignment defaults");
        Check(defaults.Effects.Values.All(e => !e.Enabled), "fresh effects are disabled");
        var medium = QualityEffectCatalog.Curve(EffectId.BuildingDurability, true);
        var mediumCost = QualityEffectCatalog.Curve(EffectId.ConsumerDemand, true);
        Check(medium[0] == .6f && medium[2] == 1f && medium[6] == 2.5f && medium.Zip(medium.Skip(1), (a, b) => b > a).All(v => v), "medium curve endpoints, neutral Normal and increasing benefits");
        Check(medium.Zip(mediumCost, (benefit, cost) => Math.Abs(benefit * cost - 1f) < .00001f).All(v => v), "medium costs use reciprocal benefits");
        var legacy = QualityConfiguration.MigrateLegacyXml(@"<ModSettings>
          <useTableQuality>False</useTableQuality><tableFactor>0.7</tableFactor><supplyQuality>False</supplyQuality>
          <minWeaponQuality>1</minWeaponQuality><minWeapoQuality>5</minWeapoQuality><maxWeapoQuality>6</maxWeapoQuality>
          <indivStuff>True</indivStuff><stuffDict><keys><li>Cloth</li><li>MissingModItem</li></keys><values><li>False</li><li>True</li></values></stuffDict>
          <indivWeapons>False</indivWeapons><weapDict><keys><li>OldGun</li></keys><values><li>False</li></values></weapDict>
        </ModSettings>");
        Check(!legacy.Production.useTableQuality && !legacy.Production.multSupplyFactor, "explicit false scalars retained");
        Check(legacy.Production.tableFactor == .7f, "table weighting retained");
        Check(legacy.Production.minWeaponQuality == 1 && legacy.Production.maxWeaponQuality == 6, "canonical weapon key wins and typo fallback remains");
        Check(!legacy.Assignment.Groups["Shell"], "fresh absent shell default excluded");
        Check(legacy.Assignment.Items["Cloth"] == RuleChoice.Exclude && legacy.Assignment.Items["MissingModItem"] == RuleChoice.Include, "true and false legacy entries retained");
        Check(!legacy.Assignment.Items.ContainsKey("OldGun") && !legacy.InactiveLegacy["weapons:OldGun"], "disabled mode recoverable and inactive");
        Check(legacy.Effects.Values.All(e => !e.Enabled), "upgrades add no enabled effects");
        var roundtrip = QualityConfiguration.Parse(legacy.Serialize());
        Check(roundtrip.Serialize() == legacy.Serialize(), "schema roundtrip is idempotent including missing mods");
        var clone = legacy.Clone();
        clone.Assignment.Items.Clear(); clone.Effects["BuildingDurability"].Factors[0] = 2;
        Check(legacy.Assignment.Items.Count == 2 && legacy.Effects["BuildingDurability"].Factors[0] == .95f, "clone does not share rules or arrays");
        var malformed = QualityConfiguration.Parse("<quality><production><tableFactor>bad</tableFactor><useTableQuality>False</useTableQuality></production><assignment><items><rule id='Gone'>Exclude</rule><rule>Include</rule></items></assignment></quality>");
        Check(malformed.Production.tableFactor == .4f && !malformed.Production.useTableQuality && malformed.Assignment.Items["Gone"] == RuleChoice.Exclude, "malformed field does not discard unrelated data");
        malformed.Production.tableFactor = 1.2f;
        malformed.Effects["BuildingDurability"].Factors[2] = float.NaN;
        Check(malformed.Validate().Contains("tableFactor") && malformed.Validate().Contains("BuildingDurability"), "weighting and nonfinite factors rejected");
        var session = new QualityConfigurationSession(legacy);
        session.Draft.Production.tableFactor = .2f;
        Check(session.Saved.Production.tableFactor == .7f && session.Active.Production.tableFactor == .7f, "draft isolated from saved and active");
        Check(session.Save().Count == 0 && session.Saved.Production.tableFactor == .2f && session.Active.Production.tableFactor == .7f, "save only changes next-session state");
        session.Draft.Production.tableFactor = .9f; session.Discard();
        Check(session.Draft.Production.tableFactor == .2f, "discard restores saved state");
        var undoDraft = session.Draft.Clone();
        undoDraft.Production.tableFactor = .6f;
        session.ReplaceDraft(undoDraft);
        undoDraft.Production.tableFactor = .9f;
        Check(session.Draft.Production.tableFactor == .6f && session.Saved.Production.tableFactor == .2f && session.Active.Production.tableFactor == .7f, "undo replaces only draft and clones its snapshot");
        session.Draft.Production.tableFactor = float.PositiveInfinity;
        Check(session.Save().Count != 0 && session.Saved.Production.tableFactor == .2f, "invalid save leaves saved intact");
        Console.WriteLine("Resolver, migration, roundtrip, validation and draft isolation checks passed.");
    }
}
