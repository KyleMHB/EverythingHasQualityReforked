using System;
using System.Linq;
using QualityEverything;
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
