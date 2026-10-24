using System;
using System.Linq;
using QualityEverything;
class SettingsChecks
{
    static void Check(bool actual, string name) { if (!actual) throw new Exception(name); }
    static void Main()
    {
        var itemSession = new QualityConfigurationSession(new QualityConfiguration());
        var itemEditor = new QualityEditorActions(itemSession);
        var editable = new RuleTarget("Workbench", "Building", true, new CategoryPath[0]);
        Check(itemEditor.SetItemRule(EditorRule.Assignment, editable, false, false, RuleChoice.Include), "item menu enables eligible assignment");
        Check(itemSession.Draft.Assignment.Items[editable.Id] == RuleChoice.Include && !itemSession.Saved.Assignment.Items.ContainsKey(editable.Id) && !itemSession.Active.Assignment.Items.ContainsKey(editable.Id), "item command edits draft only");
        Check(itemEditor.Undo() && !itemSession.Draft.Assignment.Items.ContainsKey(editable.Id) && !itemEditor.CanUndo, "one item choice produces one undo");
        Check(!itemEditor.SetItemRule(EditorRule.Assignment, editable, true, true, RuleChoice.Include), "assignment cannot replace native quality");
        Check(!itemEditor.SetItemRule(EditorRule.Price, editable, true, false, RuleChoice.Exclude) && !itemEditor.SetItemRule(EditorRule.Removal, editable, true, false, RuleChoice.Include), "provider quality and pricing protected at command boundary");
        Check(itemEditor.SetItemRule(EditorRule.Removal, editable, true, true, RuleChoice.Include) && !QualityEditorActions.HasQuality(itemSession.Draft, editable, true, true), "eligible removal changes draft outcome");
        Check(itemEditor.SetItemRule(EditorRule.Removal, editable, true, true, RuleChoice.Exclude) && QualityEditorActions.HasQuality(itemSession.Draft, editable, true, true), "always keep restores vanilla draft quality");
        itemEditor.SetItemRule(EditorRule.Participation, editable, true, true, RuleChoice.Exclude);
        Check(itemEditor.SetItemRule(EditorRule.Removal, editable, true, true, RuleChoice.Inherit) && !itemSession.Draft.VanillaRemoval.Items.ContainsKey(editable.Id) && itemSession.Draft.Participation.Items.ContainsKey(editable.Id), "use removal rule touches only removal exception");
        var absent = new RuleTarget("MissingMod", "", false, new CategoryPath[0]);
        itemSession.Draft.MarketValue.Items[absent.Id] = RuleChoice.Include;
        Check(!itemEditor.SetItemRule(EditorRule.Price, absent, false, false, RuleChoice.Exclude) && itemEditor.SetItemRule(EditorRule.Price, absent, false, false, RuleChoice.Inherit), "missing definitions allow cleanup but no unsupported writes");
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
        var sharedXml = new System.Xml.Linq.XElement("ModSettings", new System.Xml.Linq.XAttribute("Class", "QualityEverything.ModSettings_QEverything"),
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
        var editsSession = new QualityConfigurationSession(defaults);
        var edits = new QualityEditorActions(editsSession);
        edits.Edit(c => c.Production.tableFactor = .8f);
        Check(edits.CanUndo && edits.Undo() && editsSession.Draft.Production.tableFactor == .4f,
            "editor edit captures the previous draft before mutation");
        edits.Edit(c => c.Production.tableFactor = .4f);
        Check(!edits.CanUndo, "no-op editor actions create no undo step");
        edits.BeginGesture("worktable");
        edits.Edit(c => c.Production.tableFactor = .6f);
        edits.Edit(c => c.Production.tableFactor = .9f);
        edits.EndGesture();
        Check(edits.Undo() && editsSession.Draft.Production.tableFactor == .4f && !edits.CanUndo,
            "completed slider gesture is one undo step from before the first movement");
        edits.BeginGesture("strength");
        edits.Edit(c => c.Effects["BuildingDurability"].Factors[2] = 1.5f);
        edits.Edit(c => c.Effects["BuildingDurability"].Factors[2] = 1f);
        edits.EndGesture();
        Check(!edits.CanUndo, "gesture returning to its initial value creates no history");
        edits.EditNumber("production:tableFactor", "-", .4f, v => editsSession.Draft.Production.tableFactor = v);
        Check(edits.HasInvalidInput && edits.NumberText("production:tableFactor", .4f) == "-"
            && editsSession.Draft.Production.tableFactor == .4f && edits.Save().Count > 0,
            "partial numeric input stays visible and blocks Save without replacing the last valid draft");
        edits.EditNumber("production:tableFactor", "0.7", .4f, v => editsSession.Draft.Production.tableFactor = v);
        Check(!edits.HasInvalidInput && edits.Undo() && edits.NumberText("production:tableFactor", .4f) == "0.4",
            "undo synchronizes exact numeric fields after a recovered edit");
        edits.Edit(c => c.Production.tableFactor = .8f);
        edits.Discard();
        Check(!edits.CanUndo && editsSession.Draft.Production.tableFactor == .4f && !edits.HasInvalidInput,
            "discard restores saved state and clears edit history and buffers");
        edits.Edit(c => c.Production.tableFactor = .7f);
        edits.Reset();
        Check(editsSession.Draft.Production.tableFactor == .4f && edits.Undo() && editsSession.Draft.Production.tableFactor == .7f,
            "restoring defaults is undoable");
        Check(edits.Save(() => { throw new System.IO.IOException("Disk full"); }).Count > 0
            && editsSession.Draft.Production.tableFactor == .7f && editsSession.Saved.Production.tableFactor == .4f
            && editsSession.Active.Production.tableFactor == .4f && edits.CanUndo,
            "persistence failure keeps draft, saved, active and undo intact");
        Check(edits.Save(() => { }).Count == 0 && editsSession.Saved.Production.tableFactor == .7f
            && editsSession.Active.Production.tableFactor == .4f && !edits.CanUndo,
            "successful persistence updates saved only and clears undo");
        edits.Edit(c => c.Assignment.Items["MissingModItem"] = RuleChoice.Exclude);
        edits.Edit(c => c.Effects["BuildingDurability"].Factors[2] = 1.234567f);
        edits.BeginGesture("worktable");
        edits.Edit(c => c.Production.tableFactor = .1f);
        edits.Edit(c => c.Production.tableFactor = .2f);
        edits.EndGesture();
        Check(edits.Undo() && editsSession.Draft.Production.tableFactor == .7f
            && editsSession.Draft.Assignment.Items["MissingModItem"] == RuleChoice.Exclude
            && editsSession.Draft.Effects["BuildingDurability"].Factors[2] == 1.234567f,
            "gesture undo preserves exact custom Normal and unavailable item rules");
        edits.Discard();
        for (int i = 0; i < 35; i++) edits.Edit(c => c.Production.tableFactor = i / 100f);
        int undoCount = 0;
        while (edits.Undo()) undoCount++;
        Check(undoCount == 30 && editsSession.Draft.Production.tableFactor == .04f,
            "editor retains the most recent thirty complete snapshots");
        var closeSession = new QualityConfigurationSession(defaults);
        var closeEditor = new QualityEditorActions(closeSession);
        Check(!closeEditor.NeedsCloseConfirmation, "clean editor closes without a prompt");
        closeEditor.EditNumber("production:tableFactor", "-", .4f, v => closeSession.Draft.Production.tableFactor = v);
        Check(closeEditor.NeedsCloseConfirmation
            && !closeEditor.TryClose(EditorCloseAction.Save, out var closeErrors) && closeErrors.Count > 0,
            "invalid partial input requires a prompt and cannot close through Save");
        Check(!closeEditor.TryClose(EditorCloseAction.KeepEditing, out closeErrors)
            && closeEditor.NumberText("production:tableFactor", .4f) == "-",
            "keep editing retains partial input");
        Check(closeEditor.TryClose(EditorCloseAction.Discard, out closeErrors)
            && !closeEditor.NeedsCloseConfirmation && !closeEditor.HasInvalidInput,
            "discard closes and clears partial input");
        closeEditor.Edit(c => c.Production.tableFactor = .9f);
        Check(!closeEditor.TryClose(EditorCloseAction.Save, out closeErrors, () => { throw new System.IO.IOException("Disk full"); })
            && closeErrors.Any(e => e.Contains("Disk full")) && closeEditor.NeedsCloseConfirmation
            && closeSession.Draft.Production.tableFactor == .9f && closeSession.Saved.Production.tableFactor == .4f,
            "failed close-save retains the draft and a useful persistence error");
        Check(closeEditor.TryClose(EditorCloseAction.Save, out closeErrors, () => { })
            && !closeEditor.NeedsCloseConfirmation && closeSession.Saved.Production.tableFactor == .9f
            && closeSession.Active.Production.tableFactor == .4f,
            "successful close-save persists next-session settings while leaving Active unchanged");
        var summary = defaults.Clone();
        summary.Assignment.Default = true;
        summary.VanillaRemoval.Default = true;
        var summaryTarget = new RuleTarget("SummaryGun", "Weapon", true, new CategoryPath[0]);
        Check(!QualityEditorActions.HasQuality(summary, summaryTarget, true, true)
            && QualityEditorActions.HasQuality(summary, summaryTarget, true, false)
            && QualityEditorActions.HasQuality(summary, summaryTarget, false, false),
            "summary includes native and assigned quality but excludes planned native removal even with assignment enabled");
        ScopeCommandChecks();
        Console.WriteLine("Resolver, migration, roundtrip, validation and draft isolation checks passed.");
    }
    private static void ScopeCommandChecks()
    {
        var config = new QualityConfiguration();
        config.Assignment.Items["Hidden"] = RuleChoice.Exclude;
        config.Assignment.Categories["Child"] = RuleChoice.Exclude;
        var session = new QualityConfigurationSession(config);
        var editor = new QualityEditorActions(session);
        Check(editor.SetScopeRule(EditorRule.Assignment, "Stuff", "", RuleChoice.Include)
            && session.Draft.Assignment.Items.Count == 1
            && session.Draft.Assignment.Items["Hidden"] == RuleChoice.Exclude
            && session.Draft.Assignment.Categories["Child"] == RuleChoice.Exclude,
            "scope commands preserve deeper rules and exceptions without stamping items");
        Check(!session.Saved.Assignment.Groups.ContainsKey("Stuff") && !session.Active.Assignment.Groups.ContainsKey("Stuff")
            && !editor.SetScopeRule(EditorRule.Assignment, "Stuff", "Child", RuleChoice.Include),
            "scope commands isolate Draft and reject an ambiguous scope");
        var plain = new RuleTarget("Plain", "Stuff", true, new CategoryPath[0]);
        Check(QualityRules.Resolve(plain, session.Draft.Assignment).Enabled,
            "group command changes governing fallback");
        editor.SetScopeRule(EditorRule.Assignment, "", "", RuleChoice.Include);
        editor.SetScopeRule(EditorRule.Assignment, "Stuff", "", RuleChoice.Exclude);
        editor.SetScopeRule(EditorRule.Assignment, "Stuff", "", RuleChoice.Inherit);
        Check(!session.Draft.Assignment.Groups.ContainsKey("Stuff")
            && QualityRules.Resolve(plain, session.Draft.Assignment).Enabled
            && !editor.SetScopeRule(EditorRule.Assignment, "", "", RuleChoice.Inherit),
            "use parent removes group rule but global has no invented parent");
        editor.SetScopeRule(EditorRule.Assignment, "", "Parent", RuleChoice.Include);
        editor.SetScopeRule(EditorRule.Assignment, "", "Peer", RuleChoice.Exclude);
        var hidden = new RuleTarget("Hidden", "Stuff", true, new[] { new CategoryPath("Parent", 2), new CategoryPath("Peer", 2) });
        session.Draft.Participation.Items["Hidden"] = RuleChoice.Include;
        session.Draft.Assignment.Items["Other"] = RuleChoice.Include;
        session.Draft.Assignment.Items["MissingMod"] = RuleChoice.Exclude;
        var catalog = new[] { plain, hidden, new RuleTarget("Other", "Building", true, new CategoryPath[0]) };
        Check(editor.ScopeItemExceptions(EditorRule.Assignment, "", "Parent", catalog).SequenceEqual(new[] { "Hidden" })
            && editor.ClearScopeItemExceptions(EditorRule.Assignment, "", "Parent", catalog),
            "scope exception preview and clear include hidden results in the entire branch");
        var resolution = QualityRules.Resolve(hidden, session.Draft.Assignment);
        Check(!resolution.Enabled && resolution.Conflict && resolution.SourceId == "Peer"
            && session.Draft.Assignment.Items.Count == 2 && session.Draft.Participation.Items.ContainsKey("Hidden")
            && session.Draft.Assignment.Categories["Parent"] == RuleChoice.Include,
            "clearing only selected rule item exceptions preserves category conflicts and other rule sets");
        editor.SetScopeRule(EditorRule.Assignment, "", "Peer", RuleChoice.Inherit);
        Check(QualityRules.Resolve(hidden, session.Draft.Assignment).Enabled,
            "category use-parent restores remaining category fallback");
        Check(editor.ScopeItemExceptions(EditorRule.Assignment, "", "", catalog).Length == 2
            && editor.ClearScopeItemExceptions(EditorRule.Assignment, "", "", catalog)
            && session.Draft.Assignment.Items.Count == 0 && editor.Undo()
            && session.Draft.Assignment.Items.ContainsKey("MissingMod"),
            "all-items clear deliberately includes unavailable exceptions and Undo restores them");
        var assignmentBefore = session.Draft.Assignment.Clone();
        editor.SetScopeRule(EditorRule.Participation, "Stuff", "", RuleChoice.Exclude);
        editor.SetScopeRule(EditorRule.Price, "", "Parent", RuleChoice.Exclude);
        Check(!session.Draft.Participation.Groups["Stuff"] && session.Draft.MarketValue.Categories["Parent"] == RuleChoice.Exclude
            && session.Draft.Assignment.Default == assignmentBefore.Default
            && session.Draft.Assignment.Items["MissingMod"] == RuleChoice.Exclude,
            "mod-effects and price scope commands are isolated from assignment and unavailable exceptions");
    }
}
