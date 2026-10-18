using System.Collections.Generic;
using Verse;

namespace QualityEverything
{
    public class ModSettings_QEverything : ModSettings
    {

        public static bool useMaterialQuality = true;
        public static bool useTableQuality = true;
        public static bool useSkillReq = true;
        public static int stdSupplyQuality = 2;
        public static float tableFactor = .4f;

        public static bool inspiredButchering = true;
        public static bool inspiredChemistry = true;
        public static bool inspiredCooking = true;
        public static bool inspiredConstruction = true;
        public static bool inspiredGathering = true;
        public static bool inspiredHarvesting = true;
        public static bool inspiredMining = true;
        public static bool inspiredStonecutting = true;

        public static bool skilledAnimals = false;
        public static bool skilledButchering = false;
        public static bool skilledHarvesting = false;
        public static bool skilledMining = false;
        public static bool skilledStoneCutting = false;

        public static bool edificeQuality = true;
        public static int minEdificeQuality = 0;
        public static int maxEdificeQuality = 4;

        public static bool workQuality = true;
        public static int minWorkQuality = 0;
        public static int maxWorkQuality = 4;

        public static bool securityQuality = true;
        public static int minSecurityQuality = 0;
        public static int maxSecurityQuality = 4;

        public static bool stuffQuality = true;
        public static int minStuffQuality = 0;
        public static int maxStuffQuality = 4;

        public static bool ingredientQuality = true;
        public static int minIngQuality = 2;
        public static int maxIngQuality = 4;
        public static int minTastyQuality = 0;
        public static int maxTastyQuality = 4;

        public static bool mealQuality = false;
        public static int minMealQuality = 0;
        public static int maxMealQuality = 4;

        public static bool drugQuality = false;
        public static int minDrugQuality = 0;
        public static int maxDrugQuality = 4;

        public static bool medQuality = false;
        public static int minMedQuality = 0;
        public static int maxMedQuality = 4;

        public static bool manufQuality = true;
        public static int minManufQuality = 0;
        public static int maxManufQuality = 4;

        public static bool apparelQuality = false;
        public static int minApparelQuality = 0;
        public static int maxApparelQuality = 6;

        public static bool weaponQuality = false;
        public static int minWeaponQuality = 0;
        public static int maxWeaponQuality = 6;

        public static bool shellQuality = false;
        public static int minShellQuality = 0;
        public static int maxShellQuality = 4;

        public static bool indivStuff = false;
        public static bool indivBuildings = false;
        public static bool indivWeapons = false;
        public static bool indivApparel = false;
        public static bool indivOther = false;

        public static bool multSupplyFactor = true;
        public static float awfulSupplyFactor = .8f;
        public static float poorSupplyFactor = .9f;
        public static float normalSupplyFactor = 1f;
        public static float goodSupplyFactor = 1.1f;
        public static float excSupplyFactor = 1.2f;
        public static float masterSupplyFactor = 1.3f;
        public static float legSupplyFactor = 1.4f;

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
                    Log.Warning("Everything Has Quality: settings could not be loaded; using defaults. " + error.Message);
                    QualitySettingsState.Load(new QualityConfiguration { LegacyBackup = configurationXml ?? "" });
                }
            }
        }

    }
}
