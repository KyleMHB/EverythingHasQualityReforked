using System.Collections.Generic;
using Verse;

namespace ConfigurableQuality
{
    public class ModSettings_ConfigurableQuality : ModSettings
    {

        public static bool useMaterialQuality;
        public static bool useTableQuality;
        public static bool useSkillReq;
        public static int stdSupplyQuality;
        public static float tableFactor;

        public static bool inspiredButchering;
        public static bool inspiredChemistry;
        public static bool inspiredCooking;
        public static bool inspiredConstruction;
        public static bool inspiredGathering;
        public static bool inspiredHarvesting;
        public static bool inspiredMining;
        public static bool inspiredStonecutting;

        public static bool skilledAnimals;
        public static bool skilledButchering;
        public static bool skilledHarvesting;
        public static bool skilledMining;
        public static bool skilledStoneCutting;

        public static bool edificeQuality;
        public static int minEdificeQuality;
        public static int maxEdificeQuality;

        public static bool workQuality;
        public static int minWorkQuality;
        public static int maxWorkQuality;

        public static bool securityQuality;
        public static int minSecurityQuality;
        public static int maxSecurityQuality;

        public static bool stuffQuality;
        public static int minStuffQuality;
        public static int maxStuffQuality;

        public static bool ingredientQuality;
        public static int minIngQuality;
        public static int maxIngQuality;
        public static int minTastyQuality;
        public static int maxTastyQuality;

        public static bool mealQuality;
        public static int minMealQuality;
        public static int maxMealQuality;

        public static bool drugQuality;
        public static int minDrugQuality;
        public static int maxDrugQuality;

        public static bool medQuality;
        public static int minMedQuality;
        public static int maxMedQuality;

        public static bool manufQuality;
        public static int minManufQuality;
        public static int maxManufQuality;

        public static bool apparelQuality;
        public static int minApparelQuality;
        public static int maxApparelQuality;

        public static bool weaponQuality;
        public static int minWeaponQuality;
        public static int maxWeaponQuality;

        public static bool shellQuality;
        public static int minShellQuality;
        public static int maxShellQuality;

        public static bool indivStuff;
        public static bool indivBuildings;
        public static bool indivWeapons;
        public static bool indivApparel;
        public static bool indivOther;

        public static bool multSupplyFactor;
        public static float awfulSupplyFactor;
        public static float poorSupplyFactor;
        public static float normalSupplyFactor;
        public static float goodSupplyFactor;
        public static float excSupplyFactor;
        public static float masterSupplyFactor;
        public static float legSupplyFactor;

        public static Dictionary<string, bool> stuffDict = new Dictionary<string, bool>();
        public static List<string> stuffKeys = new List<string>();
        public static List<bool> stuffValues = new List<bool>();

        public static Dictionary<string, bool> bldgDict = new Dictionary<string, bool>();
        public static List<string> bldgKeys = new List<string>();
        public static List<bool> bldgValues = new List<bool>();

        public static Dictionary<string, bool> weapDict = new Dictionary<string, bool>();
        public static List<string> weapKeys = new List<string>();
        public static List<bool> weapValues = new List<bool>();

        public static Dictionary<string, bool> appDict = new Dictionary<string, bool>();
        public static List<string> appKeys = new List<string>();
        public static List<bool> appValues = new List<bool>();

        public static Dictionary<string, bool> otherDict = new Dictionary<string, bool>();
        public static List<string> otherKeys = new List<string>();
        public static List<bool> otherValues = new List<bool>();

        static ModSettings_ConfigurableQuality()
        {
            // Preserve the public compatibility fields, with one source for scalar defaults.
            var defaults = new ProductionSettings();
            foreach (var field in typeof(ProductionSettings).GetFields())
                typeof(ModSettings_ConfigurableQuality).GetField(field.Name).SetValue(null, field.GetValue(defaults));
        }

        private string configurationXml;
        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving) configurationXml = QualitySettingsState.Saved.Serialize();
            Scribe_Values.Look(ref configurationXml, "qualityConfiguration", null);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                QualityConfiguration configuration;
                try
                {
                    if (string.IsNullOrEmpty(configurationXml))
                    {
                        configuration = QualityConfiguration.MigrateLegacyXml(Scribe.loader.curXmlParent.OuterXml);
                    }
                    else configuration = QualityConfiguration.Parse(configurationXml);
                    QualitySettingsState.Load(configuration);
                }
                catch (System.Exception error)
                {
                    Log.Warning("Configurable Quality: settings could not be loaded; using defaults. " + error.Message);
                    QualitySettingsState.Load(new QualityConfiguration { LegacyBackup = configurationXml ?? "" });
                }
            }
        }

    }
}
