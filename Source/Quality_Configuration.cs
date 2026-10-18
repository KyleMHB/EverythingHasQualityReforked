using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Xml.Linq;

namespace QualityEverything
{
    internal enum RuleChoice
    {
        Inherit, Include, Exclude
    }

    internal sealed class RuleSet
    {
        public Dictionary<string, bool> Groups = new Dictionary<string, bool>(StringComparer.Ordinal);
        public Dictionary<string, RuleChoice> Categories = new Dictionary<string, RuleChoice>(StringComparer.Ordinal);
        public Dictionary<string, RuleChoice> Items = new Dictionary<string, RuleChoice>(StringComparer.Ordinal);
        public RuleSet Clone() => new RuleSet
        {
            Groups = new Dictionary<string, bool>(Groups, StringComparer.Ordinal),
            Categories = new Dictionary<string, RuleChoice>(Categories, StringComparer.Ordinal),
            Items = new Dictionary<string, RuleChoice>(Items, StringComparer.Ordinal)
        };
    }

    internal sealed class CategoryPath
    {
        public readonly string Id;
        public readonly int Depth;
        public CategoryPath(string id, int depth)
        {
            Id = id;
            Depth = depth;
        }
    }

    internal sealed class RuleTarget
    {
        public readonly string Id, Group;
        public readonly bool Supported;
        public readonly CategoryPath[] Categories;
        public RuleTarget(string id, string group, bool supported, CategoryPath[] categories)
        {
            Id = id;
            Group = group;
            Supported = supported;
            Categories = categories;
        }
    }

    internal sealed class RuleResolution
    {
        public readonly bool Enabled, Conflict;
        public readonly string ReasonKind, SourceId;
        public RuleResolution(bool enabled, string reason, string source, bool conflict = false)
        {
            Enabled = enabled;
            ReasonKind = reason;
            SourceId = source;
            Conflict = conflict;
        }
    }

    internal static class QualityRules
    {
        public static RuleResolution Resolve(RuleTarget item, RuleSet rules)
        {
            if (item == null || !item.Supported) return new RuleResolution(false, "Unsupported", item?.Id);
            RuleChoice choice;
            if (rules.Items.TryGetValue(item.Id, out choice) && choice != RuleChoice.Inherit) return new RuleResolution(choice == RuleChoice.Include, "Item", item.Id);
            int depth = -1;
            string source = null, excludedSource = null;
            bool include = false, exclude = false;
            foreach (var category in item.Categories)
            {
                if (!rules.Categories.TryGetValue(category.Id, out choice) || choice == RuleChoice.Inherit || category.Depth < depth) continue;
                if (category.Depth > depth)
                {
                    depth = category.Depth;
                    source = null;
                    excludedSource = null;
                    include = false;
                    exclude = false;
                }
                include |= choice == RuleChoice.Include;
                exclude |= choice == RuleChoice.Exclude;
                if (choice == RuleChoice.Exclude && (excludedSource == null || StringComparer.Ordinal.Compare(category.Id, excludedSource) < 0)) excludedSource = category.Id;
                // A stable winning identifier makes the explanation independent of enumeration order too.
                if (source == null || StringComparer.Ordinal.Compare(category.Id, source) < 0) source = category.Id;
            }
            if (depth >= 0) return new RuleResolution(include && !exclude, "Category", exclude ? excludedSource : source, include && exclude);
            return new RuleResolution(rules.Groups.TryGetValue(item.Group, out bool enabled) && enabled, "Group", item.Group);
        }
    }

    internal sealed class ProductionSettings
    {
        public bool useMaterialQuality = true;
        public bool useTableQuality = true;
        public bool useSkillReq = true;
        public int stdSupplyQuality = 2;
        public float tableFactor = .4f;
        public bool inspiredButchering = true;
        public bool inspiredChemistry = true;
        public bool inspiredCooking = true;
        public bool inspiredConstruction = true;
        public bool inspiredGathering = true;
        public bool inspiredHarvesting = true;
        public bool inspiredMining = true;
        public bool inspiredStonecutting = true;
        public bool skilledAnimals = false;
        public bool skilledButchering = false;
        public bool skilledHarvesting = false;
        public bool skilledMining = false;
        public bool skilledStoneCutting = false;
        public bool edificeQuality = true;
        public int minEdificeQuality = 0;
        public int maxEdificeQuality = 4;
        public bool workQuality = true;
        public int minWorkQuality = 0;
        public int maxWorkQuality = 4;
        public bool securityQuality = true;
        public int minSecurityQuality = 0;
        public int maxSecurityQuality = 4;
        public bool stuffQuality = true;
        public int minStuffQuality = 0;
        public int maxStuffQuality = 4;
        public bool ingredientQuality = true;
        public int minIngQuality = 2;
        public int maxIngQuality = 4;
        public int minTastyQuality = 0;
        public int maxTastyQuality = 4;
        public bool mealQuality = false;
        public int minMealQuality = 0;
        public int maxMealQuality = 4;
        public bool drugQuality = false;
        public int minDrugQuality = 0;
        public int maxDrugQuality = 4;
        public bool medQuality = false;
        public int minMedQuality = 0;
        public int maxMedQuality = 4;
        public bool manufQuality = true;
        public int minManufQuality = 0;
        public int maxManufQuality = 4;
        public bool apparelQuality = false;
        public int minApparelQuality = 0;
        public int maxApparelQuality = 6;
        public bool weaponQuality = false;
        public int minWeaponQuality = 0;
        public int maxWeaponQuality = 6;
        public bool shellQuality = false;
        public int minShellQuality = 0;
        public int maxShellQuality = 4;
        public bool multSupplyFactor = true;
        public float awfulSupplyFactor = .8f;
        public float poorSupplyFactor = .9f;
        public float normalSupplyFactor = 1f;
        public float goodSupplyFactor = 1.1f;
        public float excSupplyFactor = 1.2f;
        public float masterSupplyFactor = 1.3f;
        public float legSupplyFactor = 1.4f;
        public ProductionSettings Clone() => (ProductionSettings)MemberwiseClone();
    }

    internal enum EffectId
    {
        BuildingDurability, WeaponDurability, ApparelDurability, WorktableSpeed, ResearchSpeed, DoorSpeed, GeneratorOutput, ConsumerDemand, BatteryCapacity, BatteryEfficiency, FoodNutrition, FoodPoisoningRisk, MedicinePotency, DrugStrength, DrugDuration, EquipmentWork, EquipmentMovement, EquipmentMedicine, EquipmentSocial, EquipmentAbilities, MeleeCooldown, RangedCooldown, TurretCooldown, TrapDamage, LinkedFurniture
    }

    internal sealed class EffectSettings
    {
        public bool Enabled;
        public float[] Factors =
      {
 .95f,.98f,1f,1.02f,1.05f,1.1f,1.2f
};
        public EffectSettings Clone() => new EffectSettings
        {
            Enabled = Enabled,
            Factors = (float[])Factors.Clone()
        };
    }

    internal sealed class QualityConfiguration
    {
        public ProductionSettings Production = new ProductionSettings();
        public RuleSet Assignment = new RuleSet();
        public RuleSet Participation = new RuleSet();
        public Dictionary<string, EffectSettings> Effects = new Dictionary<string, EffectSettings>(StringComparer.Ordinal);
        public Dictionary<string, bool> InactiveLegacy = new Dictionary<string, bool>(StringComparer.Ordinal);
        public string LegacyBackup = "";
        public readonly List<string> RecoveredFields = new List<string>();
        public float LinkedFurnitureBonusCap = .02f, LinkedFurniturePenaltyCap = .005f;
        public QualityConfiguration()
        {
            foreach (string group in new[] { "Stuff","WorkBuilding","SecurityBuilding","Building","Weapon","Shell","Apparel","Drug","Medicine","Meal","TastyIngredient","Ingredient","Manufactured" }
         )
            {
                Assignment.Groups[group] = group == "Stuff" || group == "WorkBuilding" || group == "SecurityBuilding" || group == "Building" || group == "TastyIngredient" || group == "Ingredient" || group == "Manufactured";
                Participation.Groups[group] = true;
            }
            foreach (EffectId id in Enum.GetValues(typeof(EffectId))) Effects[id.ToString()] = new EffectSettings
            {
                Factors = QualityEffectCatalog.Curve(id)
            };
        }
        public QualityConfiguration Clone() => new QualityConfiguration
        {
            Production = Production.Clone(),
            Assignment = Assignment.Clone(),
            Participation = Participation.Clone(),
            Effects = Effects.ToDictionary(p => p.Key, p => p.Value.Clone(), StringComparer.Ordinal),
            InactiveLegacy = new Dictionary<string, bool>(InactiveLegacy, StringComparer.Ordinal),
            LegacyBackup = LegacyBackup,
            LinkedFurnitureBonusCap = LinkedFurnitureBonusCap,
            LinkedFurniturePenaltyCap = LinkedFurniturePenaltyCap
        };
        public List<string> Validate()
        {
            var errors = new List<string>();
            foreach (var field in typeof(ProductionSettings).GetFields())
            {
                object value = field.GetValue(Production);
                if (value is float number && (float.IsNaN(number) || float.IsInfinity(number) || number < 0 || number > 10)) errors.Add(field.Name);
                if (field.Name == "tableFactor" && (float)value > 1) errors.Add(field.Name);
                if (value is int tier && (tier < 0 || tier > 6)) errors.Add(field.Name);
                if (field.Name.StartsWith("min"))
                {
                    var maximum = typeof(ProductionSettings).GetField("max" + field.Name.Substring(3));
                    if (maximum != null && (int)value > (int)maximum.GetValue(Production)) errors.Add(field.Name);
                }
            }
            foreach (var pair in Effects) if (pair.Value == null || pair.Value.Factors == null || pair.Value.Factors.Length != 7 || pair.Value.Factors.Any(v => float.IsNaN(v) || float.IsInfinity(v) || v < .01f || v > 10f)) errors.Add(pair.Key);
            if (float.IsNaN(LinkedFurnitureBonusCap) || LinkedFurnitureBonusCap < 0 || LinkedFurnitureBonusCap > .02f) errors.Add("LinkedFurnitureBonusCap");
            if (float.IsNaN(LinkedFurniturePenaltyCap) || LinkedFurniturePenaltyCap < 0 || LinkedFurniturePenaltyCap > .005f) errors.Add("LinkedFurniturePenaltyCap");
            return errors;
        }
        public string Serialize()
        {
            var root = new XElement("quality", new XAttribute("schema", 1));
            root.Add(new XElement("production", typeof(ProductionSettings).GetFields().Select(f => new XElement(f.Name, Convert.ToString(f.GetValue(Production), CultureInfo.InvariantCulture)))));
            root.Add(WriteRules("assignment", Assignment), WriteRules("participation", Participation));
            root.Add(new XElement("effects", Effects.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new XElement("effect", new XAttribute("id", p.Key), new XAttribute("enabled", p.Value.Enabled), string.Join(",", p.Value.Factors.Select(v => v.ToString("R", CultureInfo.InvariantCulture)))))));
            root.Add(new XElement("inactive", InactiveLegacy.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new XElement("item", new XAttribute("id", p.Key), p.Value))));
            root.Add(new XElement("legacyBackup", LegacyBackup), new XElement("linkedBonusCap", LinkedFurnitureBonusCap.ToString("R", CultureInfo.InvariantCulture)), new XElement("linkedPenaltyCap", LinkedFurniturePenaltyCap.ToString("R", CultureInfo.InvariantCulture)));
            return root.ToString(SaveOptions.DisableFormatting);
        }
        static XElement WriteRules(string name, RuleSet rules) => new XElement(name,
         new XElement("groups", rules.Groups.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new XElement("rule", new XAttribute("id", p.Key), p.Value))),
         new XElement("categories", rules.Categories.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new XElement("rule", new XAttribute("id", p.Key), p.Value))),
         new XElement("items", rules.Items.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new XElement("rule", new XAttribute("id", p.Key), p.Value))));
        static void ReadRules(XElement node, RuleSet rules)
        {
            if (node == null) return;
            foreach (var el in node.Element("groups")?.Elements() ?? Enumerable.Empty<XElement>()) if (!string.IsNullOrEmpty((string)el.Attribute("id")) && bool.TryParse(el.Value, out bool b)) rules.Groups[(string)el.Attribute("id")] = b;
            foreach (var kind in new[] { "categories","items" }
         ) foreach (var el in node.Element(kind)?.Elements() ?? Enumerable.Empty<XElement>()) if (!string.IsNullOrEmpty((string)el.Attribute("id")) && Enum.TryParse(el.Value, out RuleChoice r) && Enum.IsDefined(typeof(RuleChoice), r)) (kind == "categories" ? rules.Categories : rules.Items)[(string)el.Attribute("id")] = r;
        }
        public static QualityConfiguration Parse(string xml)
        {
            var result = new QualityConfiguration();
            if (string.IsNullOrEmpty(xml)) return result;
            var root = XElement.Parse(xml);
            foreach (var field in typeof(ProductionSettings).GetFields())
            {
                string text = root.Element("production")?.Element(field.Name)?.Value;
                if (text != null)
                {
                    if (field.FieldType == typeof(bool) && bool.TryParse(text, out bool boolean)) field.SetValue(result.Production, boolean);
                    else if (field.FieldType == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer)) field.SetValue(result.Production, integer);
                    else if (field.FieldType == typeof(float) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)) field.SetValue(result.Production, number);
                    else result.RecoveredFields.Add(field.Name);
                }
            }
            ReadRules(root.Element("assignment"), result.Assignment);
            ReadRules(root.Element("participation"), result.Participation);
            foreach (var effect in root.Element("effects")?.Elements() ?? Enumerable.Empty<XElement>())
            {
                string id = (string)effect.Attribute("id");
                if (string.IsNullOrEmpty(id)) continue;
                var settings = result.Effects.TryGetValue(id, out var existing) ? existing : new EffectSettings();
                if (bool.TryParse((string)effect.Attribute("enabled"), out bool enabled)) settings.Enabled = enabled;
                string[] numbers = effect.Value.Split(',');
                if (numbers.Length == 7) for (int i = 0; i < 7; i++)
                    {
                        if (float.TryParse(numbers[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float number)) settings.Factors[i] = number;
                        else result.RecoveredFields.Add(id);
                    }
                else result.RecoveredFields.Add(id);
                result.Effects[id] = settings;
            }
            foreach (var item in root.Element("inactive")?.Elements() ?? Enumerable.Empty<XElement>()) if (!string.IsNullOrEmpty((string)item.Attribute("id")) && bool.TryParse(item.Value, out bool value)) result.InactiveLegacy[(string)item.Attribute("id")] = value;
            result.LegacyBackup = root.Element("legacyBackup")?.Value ?? "";
            if (float.TryParse(root.Element("linkedBonusCap")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float bonus)) result.LinkedFurnitureBonusCap = bonus;
            if (float.TryParse(root.Element("linkedPenaltyCap")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float penalty)) result.LinkedFurniturePenaltyCap = penalty;
            return result;
        }
        public static readonly Dictionary<string, string> GroupFields = new Dictionary<string, string>
        {
            { "Stuff", "stuffQuality" }, { "WorkBuilding", "workQuality" },
            { "SecurityBuilding", "securityQuality" }, { "Building", "edificeQuality" },
            { "Weapon", "weaponQuality" }, { "Shell", "shellQuality" },
            { "Apparel", "apparelQuality" }, { "Drug", "drugQuality" },
            { "Medicine", "medQuality" }, { "Meal", "mealQuality" },
            { "TastyIngredient", "ingredientQuality" }, { "Ingredient", "ingredientQuality" },
            { "Manufactured", "manufQuality" }
        };

        public static QualityConfiguration MigrateLegacyXml(string xml)
        {
            var legacy = XElement.Parse(xml);
            var production = new XElement("production", legacy.Elements().Select(e => new XElement(e)));
            if (legacy.Element("supplyQuality") != null) production.SetElementValue("multSupplyFactor", legacy.Element("supplyQuality").Value);
            foreach (string bound in new[] { "min","max" }
         )
                if (legacy.Element(bound + "WeaponQuality") == null && legacy.Element(bound + "WeapoQuality") != null)
                    production.SetElementValue(bound + "WeaponQuality", legacy.Element(bound + "WeapoQuality").Value);
            var result = Parse(new XElement("quality", production).ToString());
            result.LegacyBackup = xml;
            foreach (var pair in GroupFields) result.Assignment.Groups[pair.Key] = (bool)typeof(ProductionSettings).GetField(pair.Value).GetValue(result.Production);
            var legacyModes = new[]
            {
                new[] { "stuff", "stuffDict", "indivStuff" },
                new[] { "buildings", "bldgDict", "indivBuildings" },
                new[] { "weapons", "weapDict", "indivWeapons" },
                new[] { "apparel", "appDict", "indivApparel" },
                new[] { "other", "otherDict", "indivOther" }
            };
            foreach (var dictionary in legacyModes)
            {
                bool enabled = bool.TryParse(legacy.Element(dictionary[2])?.Value, out bool flag) && flag;
                var keys = legacy.Element(dictionary[1])?.Element("keys")?.Elements().Select(e => e.Value).ToArray() ?? new string[0];
                var values = legacy.Element(dictionary[1])?.Element("values")?.Elements().Select(e => e.Value).ToArray() ?? new string[0];
                var entries = new Dictionary<string, bool>(StringComparer.Ordinal);
                for (int i = 0; i < Math.Min(keys.Length, values.Length); i++) if (!string.IsNullOrEmpty(keys[i]) && bool.TryParse(values[i], out bool value)) entries[keys[i]] = value;
                result.ImportLegacy(dictionary[0], entries, enabled);
            }
            return result;
        }
        public void ImportLegacy(string name, Dictionary<string, bool> values, bool enabled)
        {
            if (values == null) return;
            foreach (var pair in values) if (!string.IsNullOrEmpty(pair.Key))
                {
                    if (enabled) Assignment.Items[pair.Key] = pair.Value ? RuleChoice.Include : RuleChoice.Exclude;
                    else InactiveLegacy[name + ":" + pair.Key] = pair.Value;
                }
        }
    }

    internal sealed class QualityConfigurationSession
    {
        private readonly QualityConfiguration active;
        public QualityConfiguration Active => active.Clone();
        internal QualityConfiguration ActiveSnapshot => active;
        public QualityConfiguration Saved
        {
            get; private set;
        }
        public QualityConfiguration Draft
        {
            get; private set;
        }
        public QualityConfigurationSession(QualityConfiguration loaded)
        {
            active = loaded.Clone();
            Saved = loaded.Clone();
            Draft = loaded.Clone();
        }
        public List<string> Save()
        {
            var errors = Draft.Validate();
            if (errors.Count == 0) Saved = Draft.Clone();
            return errors;
        }
        public void Discard()
        {
            Draft = Saved.Clone();
        }
    }

    internal sealed class EffectDescriptor
    {
        public readonly EffectId Id;
        public readonly string Family, LabelKey;
        public readonly bool Cost;
        public readonly float Minimum = .01f, Maximum = 10f;
        public EffectDescriptor(EffectId id, string family)
        {
            Id = id;
            Family = family;
            LabelKey = "QEverything.Options.Effect." + id;
            Cost = QualityEffectCatalog.IsCost(id);
        }
    }

    internal static class QualityEffectCatalog
    {
        public static readonly EffectDescriptor[] Descriptors = Enum.GetValues(typeof(EffectId)).Cast<EffectId>().Select(id => new EffectDescriptor(id, Family(id))).ToArray();
        private static string Family(EffectId id)
        {
            switch (id)
            {
                case EffectId.BuildingDurability:
                case EffectId.WeaponDurability:
                case EffectId.ApparelDurability:
                    return "Durability";
                case EffectId.WorktableSpeed:
                case EffectId.ResearchSpeed:
                case EffectId.DoorSpeed:
                    return "Production";
                case EffectId.GeneratorOutput:
                case EffectId.ConsumerDemand:
                case EffectId.BatteryCapacity:
                case EffectId.BatteryEfficiency:
                    return "Power";
                case EffectId.FoodNutrition:
                case EffectId.FoodPoisoningRisk:
                case EffectId.MedicinePotency:
                case EffectId.DrugStrength:
                case EffectId.DrugDuration:
                    return "Consumables";
                case EffectId.EquipmentWork:
                case EffectId.EquipmentMovement:
                case EffectId.EquipmentMedicine:
                case EffectId.EquipmentSocial:
                case EffectId.EquipmentAbilities:
                    return "Equipment";
                case EffectId.LinkedFurniture:
                    return "Furniture";
                default:
                    return "Combat";
            }
        }
        public static bool IsCost(EffectId id) => id == EffectId.ConsumerDemand || id == EffectId.FoodPoisoningRisk || id == EffectId.MeleeCooldown || id == EffectId.RangedCooldown || id == EffectId.TurretCooldown;
        public static float[] Curve(EffectId id) => IsCost(id)
            ? new[] { 1.05f, 1.02f, 1f, .98f, .95f, .9f, .8f }
            : new[] { .95f, .98f, 1f, 1.02f, 1.05f, 1.1f, 1.2f };
    }
}
