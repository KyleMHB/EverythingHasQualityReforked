using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ConfigurableQuality;
partial class SettingsChecks
{
    static void Rejected(Action action, string reason)
    {
        bool rejected = false;
        try { action(); } catch (Exception e) when (e is IOException || e is ArgumentException || e is FormatException || e is System.Xml.XmlException) { rejected = true; }
        Check(rejected, reason);
    }
    static void CheckUserPresets()
    {
        string directory = Path.Combine(Path.GetTempPath(), "quality-presets-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new QualityPresetStore(directory, s => s.Replace("/", "_"));
            var source = new QualityConfiguration();
            source.Assignment.Items["UnavailableMod_Item"] = RuleChoice.Exclude;
            source.Effects["FutureEffect"] = new EffectSettings { Enabled = true, Factors = new[] {1f,1f,1f,1f,1f,1f,1f} };
            store.Save("My preset", source);
            Check(store.List().Single() == "My preset" && store.Load("My preset").Serialize() == source.Serialize(), "file preset preserves complete unknown entries");
            string original = store.Read("My preset");
            foreach (string name in new[] { "", " ", "..", "CON", "NUL.xml", "COM1", "foo\\bar", "A " }) Rejected(() => store.Save(name, source), "reject unusable filename");
            Rejected(() => store.Save("MY PRESET", source), "case collision requires explicit overwrite");
            store.Save("a/b", source);
            Rejected(() => store.Save("a_b", source), "sanitization collision requires explicit overwrite");
            using (var locked = new FileStream(Path.Combine(directory, "My preset.xml"), FileMode.Open, FileAccess.Read, FileShare.None))
                Rejected(() => store.Save("My preset", source, original), "actual IO failure during overwrite");
            Check(store.Read("My preset") == original, "IO failure retains prior valid file");
            source.Assignment.Items.Clear();
            Rejected(() => store.Save("My preset", source), "canceled overwrite preserves old file");
            Check(store.Read("My preset") == original, "prior file survives refused overwrite");
            Rejected(() => store.Save("My preset", source, "stale"), "stale overwrite rejected");
            store.Save("My preset", source, original);
            Check(!store.Load("My preset").Assignment.Items.Any(), "authorized atomic overwrite replaces complete snapshot");
            Rejected(() => store.Load("missing"), "missing load reports failure");
            Rejected(() => store.Delete("missing", ""), "missing delete reports failure");
            Rejected(() => store.Delete("My preset", original), "stale delete rejected");
            store.Delete("My preset", store.Read("My preset"));
            Check(!store.List().Contains("My preset"), "delete explicit preset");
            var corruptions = new Action<XElement>[] {
                x => x.Name = "wrong", x => x.SetAttributeValue("schema", 2), x => x.Element("production").Remove(),
                x => x.Element("production").Element("tableFactor").Value = "oops", x => x.Element("linkedBonusCap").Value = "NaN",
                x => x.Element("effects").Elements().First().SetAttributeValue("enabled", "oops"),
                x => x.Element("effects").Elements().First().Attribute("id").Remove(),
                x => x.Element("effects").Add(new XElement(x.Element("effects").Elements().First())),
                x => x.Element("assignment").SetAttributeValue("default", "oops"),
                x => x.Element("assignment").Element("items").Add(new XElement("rule", new XAttribute("id", "bad"), "99")),
                x => x.Element("inactive").Add(new XElement("item", new XAttribute("id", "bad"), "oops")),
                x => x.Element("linkedPenaltyCap").Remove(), x => x.Element("effects").Elements().First().Value = "1,2"
            };
            foreach (var corrupt in corruptions)
            {
                var xml = XElement.Parse(original); corrupt(xml);
                File.WriteAllText(Path.Combine(directory, "broken.xml"), xml.ToString());
                Rejected(() => store.Load("broken"), "strict snapshot rejects silent recovery or omitted data");
            }
            foreach (string bad in new[] { "", "<quality>", "<quality schema='1'/>" })
            { File.WriteAllText(Path.Combine(directory, "broken.xml"), bad); Rejected(() => store.Load("broken"), "strict snapshot rejects incomplete document"); }
            source.Effects["FutureEffect"].Factors[0] = float.NaN;
            Rejected(() => store.Save("a_b", source, store.Read("a_b")), "invalid write rejected before replacing prior file");
            Check(store.Load("a_b").Assignment.Items.ContainsKey("UnavailableMod_Item"), "failed write retains old valid preset");
            var session = new QualityConfigurationSession(new QualityConfiguration());
            var editor = new QualityEditorActions(session);
            string saved = session.Saved.Serialize(), active = session.Active.Serialize();
            session.Draft.Assignment.Items["remove-me"] = RuleChoice.Include;
            string before = session.Draft.Serialize();
            var candidate = store.Load("a_b");
            Check(session.Draft.Serialize() == before && !editor.CanUndo, "preview cancellation does not mutate draft or history");
            Check(editor.ApplyPreset(candidate) && !session.Draft.Assignment.Items.ContainsKey("remove-me"), "load replaces whole draft including removal of exceptions");
            Check(editor.MatchesPreset(candidate), "complete match recognized");
            Check(editor.Undo() && !editor.CanUndo && session.Draft.Serialize() == before && !editor.MatchesPreset(candidate), "whole preset load is one Undo and match follows draft");
            Check(session.Saved.Serialize() == saved && session.Active.Serialize() == active, "preset does not change Saved or Active");
            editor.EditNumber("linkedBonus", "-", 0, n => {});
            Check(!editor.CanSavePreset, "invalid uncommitted buffer cannot become preset");
            editor.SynchronizeInputs();
            session.Draft.MarketValue.Items["price"] = RuleChoice.Exclude;
            session.Draft.VanillaRemoval.Items["native"] = RuleChoice.Include;
            editor.ApplyBuiltinPreset(false);
            Check(session.Draft.MarketValue.Items.ContainsKey("price") && session.Draft.VanillaRemoval.Items.ContainsKey("native") && session.Draft.Assignment.Items.ContainsKey("remove-me"), "ordinary recipe preserves policy and exceptions");
            Check(session.Draft.Production.useMaterialQuality && !session.Draft.Production.skilledAnimals && session.Draft.Effects.Values.Any(e => e.Enabled), "ordinary recipe retains documented legacy production");
            var progression = editor.BuiltinPreset(false, new[] { "remove-me", "DBH_Fixture" }, id => id != EffectId.BuildingDurability);
            Check(progression.Assignment.Items["remove-me"] == RuleChoice.Exclude && progression.Participation.Items["DBH_Fixture"] == RuleChoice.Exclude && !progression.Effects["BuildingDurability"].Enabled && progression.ProgressionCompatibility, "Progression previews deliberate exceptions with actual adapter availability");
            Check(session.Draft.Assignment.Items["remove-me"] == RuleChoice.Include, "Progression preview leaves original overrides intact");
            editor.ApplyPreset(progression);
            Check(editor.Undo() && session.Draft.Assignment.Items["remove-me"] == RuleChoice.Include, "Progression application one-step Undo preserves prior overrides");

        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
