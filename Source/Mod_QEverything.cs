using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QualityEverything
{
    class Mod_QEverything : Mod
    {
        private static Vector2 texScroll = Vector2.zero;
        private static Vector2 resScroll = Vector2.zero;
        private static Vector2 bldgScroll = Vector2.zero;
        private static Vector2 bldgScroll2 = Vector2.zero;
        private static Vector2 weapScroll = Vector2.zero;
        private static Vector2 appScroll = Vector2.zero;
        private static Vector2 foodScroll = Vector2.zero;
        private static Vector2 otherScroll = Vector2.zero;
        private static Listing_Standard listing = new Listing_Standard();
        public static ModSettings_QEverything settings;
        private static int currentTab;

        // One search string per customization tab (tab 4 shares one across its
        // two side-by-side columns per design).
        private static string stuffSearch = string.Empty;
        private static string bldgSearch = string.Empty;
        private static string weapAppSearch = string.Empty;
        private static string otherSearch = string.Empty;

        // Per-dict caches. Hold pre-partitioned, pre-sorted, pre-filtered
        // entries so DoSettingsWindowContents doesn't rebuild them every frame.
        private static readonly QualityListCache stuffCache = new QualityListCache();
        private static readonly QualityListCache bldgCache = new QualityListCache();
        private static readonly QualityListCache weapCache = new QualityListCache();
        private static readonly QualityListCache appCache = new QualityListCache();
        private static readonly QualityListCache otherCache = new QualityListCache();

        public Mod_QEverything(ModContentPack content) : base(content)
        {
            settings = GetSettings<ModSettings_QEverything>();
        }

        public override string SettingsCategory()
        {
            return "Everything Has Quality";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            /*ModSettings_QEverything.stuffDict ??= new Dictionary<string, bool>();
            ModSettings_QEverything.bldgDict ??= new Dictionary<string, bool>();
            ModSettings_QEverything.weapDict ??= new Dictionary<string, bool>();
            ModSettings_QEverything.appDict ??= new Dictionary<string, bool>();
            ModSettings_QEverything.otherDict ??= new Dictionary<string, bool>();*/

            Rect labelRect = new Rect(5f, 34f, inRect.width * .5f, 42f);
            listing.Begin(labelRect);
            if (listing.ButtonText("QEverything.ApplyCat".Translate()))
            {
                Mod_SettingsUtility.ApplySettingsChanges();
            }
            listing.End();
            Rect rect = new Rect(605f, 34f, inRect.width * .3f, 42f);
            listing.Begin(rect);
            string buttonText = "QEverything.CatDef".Translate();
            if (currentTab >= 2) buttonText = "QEverything.IndDef".Translate();
            if (listing.ButtonText(buttonText, null))
            {
                if (currentTab < 2) Mod_SettingsUtility.RestoreDefaults();
                else
                {
                    ModSettings_QEverything.stuffDict.Clear();
                    ModSettings_QEverything.bldgDict.Clear();
                    ModSettings_QEverything.weapDict.Clear();
                    ModSettings_QEverything.appDict.Clear();
                    ModSettings_QEverything.otherDict.Clear();
                    ModSettings_QEverything.indivStuff = false;
                    ModSettings_QEverything.indivBuildings = false;
                    ModSettings_QEverything.indivWeapons = false;
                    ModSettings_QEverything.indivApparel = false;
                    ModSettings_QEverything.indivOther = false;
                }
            }
            listing.End();
            DoSettings(inRect);
        }

        public static void DoSettings(Rect canvas)
        {
            canvas = canvas.Rounded();
            canvas.height -= 60f;
            canvas.y += 60f;
            Widgets.DrawMenuSection(canvas);
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("QEverything.Tab0".Translate(), delegate()
                {
                    currentTab = 0;
                    settings.Write();
                },  currentTab == 0),
                new TabRecord("QEverything.Tab1".Translate(), delegate()
                {
                    currentTab = 1;
                    settings.Write();
                }, currentTab == 1),
                new TabRecord("QEverything.Tab2".Translate(), delegate()
                {
                    currentTab = 2;
                settings.Write();
                }, currentTab == 2),
                new TabRecord("QEverything.Tab3".Translate(), delegate()
                {
                    currentTab = 3;
                    settings.Write();
                }, currentTab == 3),
                new TabRecord("QEverything.Tab4".Translate(), delegate()
                {
                    currentTab = 4;
                    settings.Write();
                }, currentTab == 4),
                new TabRecord("QEverything.Tab5".Translate(), delegate()
                {
                    currentTab = 5;
                    settings.Write();
                }, currentTab == 5)
            };
            TabDrawer.DrawTabs(canvas, tabs, 200f);
            if (currentTab == 0) DoQualityByCategory(canvas.ContractedBy(10f));
            if (currentTab == 1) DoSkillsAndInspiration(canvas.ContractedBy(10f));
            if (currentTab == 2) DoCustomizeStuff(canvas);
            if (currentTab == 3) DoCustomizeBuildings(canvas);
            if (currentTab == 4) DoCustomizeWeapons(canvas.ContractedBy(10f));
            if (currentTab == 5) DoCustomizeOther(canvas);
        }

        public static void DoQualityByCategory(Rect rect)
        {
            Rect firstCol = new Rect(5f, 110f, rect.width * .35f, rect.height);
            listing.Begin(firstCol);
            listing.CheckboxLabeled("    " + "QEverything.Work".Translate(), ref ModSettings_QEverything.workQuality);
            listing.CheckboxLabeled("    " + "QEverything.Security".Translate(), ref ModSettings_QEverything.securityQuality);
            listing.CheckboxLabeled("    " + "QEverything.Edifice".Translate(), ref ModSettings_QEverything.edificeQuality);
            listing.CheckboxLabeled("    " + "QEverything.Stuff".Translate(), ref ModSettings_QEverything.stuffQuality);
            listing.CheckboxLabeled("    " + "QEverything.Manuf".Translate(), ref ModSettings_QEverything.manufQuality);
            listing.CheckboxLabeled("    " + "QEverything.Meal".Translate(), ref ModSettings_QEverything.mealQuality);
            listing.CheckboxLabeled("    " + "QEverything.Ingredients".Translate(), ref ModSettings_QEverything.ingredientQuality);
            if (ModSettings_QEverything.ingredientQuality) listing.Label("        " + "QEverything.Tasty".Translate());
            listing.CheckboxLabeled("    " + "QEverything.Drugs".Translate(), ref ModSettings_QEverything.drugQuality);
            listing.CheckboxLabeled("    " + "QEverything.Med".Translate(), ref ModSettings_QEverything.medQuality);
            listing.CheckboxLabeled("    " + "QEverything.Apparel".Translate(), ref ModSettings_QEverything.apparelQuality);
            listing.CheckboxLabeled("    " + "QEverything.Weapons".Translate(), ref ModSettings_QEverything.weaponQuality);
            listing.CheckboxLabeled("    " + "QEverything.Shells".Translate(), ref ModSettings_QEverything.shellQuality);
            listing.End();

            //Column2 - Min Quality Values
            Rect secondCol = new Rect(325f, 110f, rect.width * .3f, rect.height);
            listing.Begin(secondCol);

            string labelWork = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minWorkQuality).ToString();
            string minWorkBuffer = ModSettings_QEverything.minWorkQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelWork, ref ModSettings_QEverything.minWorkQuality, ref minWorkBuffer, 1, 0, 2);


            string labelSecurity = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minSecurityQuality).ToString();
            string minSecurityBuffer = ModSettings_QEverything.minSecurityQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelSecurity, ref ModSettings_QEverything.minSecurityQuality, ref minSecurityBuffer, 1, 0, 2);

            string labelConstruction = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minEdificeQuality).ToString();
            string minConstructionBuffer = ModSettings_QEverything.minEdificeQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelConstruction, ref ModSettings_QEverything.minEdificeQuality, ref minConstructionBuffer, 1, 0, 2);

            string labelStuff = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minStuffQuality).ToString();
            string minStuffBuffer = ModSettings_QEverything.minStuffQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelStuff, ref ModSettings_QEverything.minStuffQuality, ref minStuffBuffer, 1, 0, 2);

            string labelManuf = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minManufQuality).ToString();
            string minManufBuffer = ModSettings_QEverything.minManufQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelManuf, ref ModSettings_QEverything.minManufQuality, ref minManufBuffer, 1, 0, 2);

            string labelMeals = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minMealQuality).ToString();
            string minMealBuffer = ModSettings_QEverything.minMealQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelMeals, ref ModSettings_QEverything.minMealQuality, ref minMealBuffer, 1, 0, 2);

            string labelIng = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minIngQuality).ToString();
            string minIngBuffer = ModSettings_QEverything.minIngQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelIng, ref ModSettings_QEverything.minIngQuality, ref minIngBuffer, 1, 0, 2);

            string labelTasty = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minTastyQuality).ToString();
            string minTastyBuffer = ModSettings_QEverything.minTastyQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelTasty, ref ModSettings_QEverything.minTastyQuality, ref minTastyBuffer, 1, 0, 2);

            string labelDrug = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minDrugQuality).ToString();
            string minDrugBuffer = ModSettings_QEverything.minDrugQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelDrug, ref ModSettings_QEverything.minDrugQuality, ref minDrugBuffer, 1, 0, 2);

            string labelMed = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minMedQuality).ToString();
            string minMedBuffer = ModSettings_QEverything.minMedQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelMed, ref ModSettings_QEverything.minMedQuality, ref minMedBuffer, 1, 0, 2);

            string labelApparel = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minApparelQuality).ToString();
            string minApparelBuffer = ModSettings_QEverything.minApparelQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelApparel, ref ModSettings_QEverything.minApparelQuality, ref minApparelBuffer, 1, 0, 2);

            string labelWeapons = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minWeaponQuality).ToString();
            string minWeaponBuffer = ModSettings_QEverything.minWeaponQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelWeapons, ref ModSettings_QEverything.minWeaponQuality, ref minWeaponBuffer, 1, 0, 2);

            string labelShell = "QEverything.Min".Translate() + ((QualityCategory)ModSettings_QEverything.minShellQuality).ToString();
            string minShellBuffer = ModSettings_QEverything.minShellQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelShell, ref ModSettings_QEverything.minShellQuality, ref minShellBuffer, 1, 0, 2);

            listing.End();

            //Column 3 - Max Qualiyt Values
            Rect thirdCol = new Rect(600f, 110f, rect.width * .3f, rect.height);
            listing.Begin(thirdCol);

            string labelWork2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxWorkQuality).ToString();
            string maxWorkBuffer = ModSettings_QEverything.maxWorkQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelWork2, ref ModSettings_QEverything.maxWorkQuality, ref maxWorkBuffer, 1, 2, 6);

            string labelSecurity2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxSecurityQuality).ToString();
            string maxSecurityBuffer = ModSettings_QEverything.maxSecurityQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelSecurity2, ref ModSettings_QEverything.maxSecurityQuality, ref maxSecurityBuffer, 1, 2, 6);

            string labelConstruction2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxEdificeQuality).ToString();
            string maxConstructionBuffer = ModSettings_QEverything.maxEdificeQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelConstruction2, ref ModSettings_QEverything.maxEdificeQuality, ref maxConstructionBuffer, 1, 2, 6);

            string labelStuff2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxStuffQuality).ToString();
            string maxStuffBuffer = ModSettings_QEverything.maxStuffQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelStuff2, ref ModSettings_QEverything.maxStuffQuality, ref maxStuffBuffer, 1, 2, 6);

            string labelManuf2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxManufQuality).ToString();
            string maxManufBuffer = ModSettings_QEverything.maxManufQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelManuf2, ref ModSettings_QEverything.maxManufQuality, ref maxManufBuffer, 1, 2, 6);

            string labelMeals2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxMealQuality).ToString();
            string maxMealBuffer = ModSettings_QEverything.maxMealQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelMeals2, ref ModSettings_QEverything.maxMealQuality, ref maxMealBuffer, 1, 2, 6);

            string labelIng2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxIngQuality).ToString();
            string maxIngBuffer = ModSettings_QEverything.maxIngQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelIng2, ref ModSettings_QEverything.maxIngQuality, ref maxIngBuffer, 1, 2, 6);

            string labelTasty2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxTastyQuality).ToString();
            string maxTastyBuffer = ModSettings_QEverything.maxTastyQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelTasty2, ref ModSettings_QEverything.maxTastyQuality, ref maxTastyBuffer, 1, 2, 6);

            string labelDrug2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxDrugQuality).ToString();
            string maxDrugBuffer = ModSettings_QEverything.maxDrugQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelDrug2, ref ModSettings_QEverything.maxDrugQuality, ref maxDrugBuffer, 1, 2, 6);

            string labelMed2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxMedQuality).ToString();
            string maxMedBuffer = ModSettings_QEverything.maxMedQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelMed2, ref ModSettings_QEverything.maxMedQuality, ref maxMedBuffer, 1, 2, 6);

            string labelApparel2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxApparelQuality).ToString();
            string maxApparelBuffer = ModSettings_QEverything.maxApparelQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelApparel2, ref ModSettings_QEverything.maxApparelQuality, ref maxApparelBuffer, 1, 2, 6);

            string labelWeapons2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxWeaponQuality).ToString();
            string maxWeaponBuffer = ModSettings_QEverything.maxWeaponQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelWeapons2, ref ModSettings_QEverything.maxWeaponQuality, ref maxWeaponBuffer, 1, 2, 6);

            string labelShell2 = "QEverything.Max".Translate() + ((QualityCategory)ModSettings_QEverything.maxShellQuality).ToString();
            string maxShellBuffer = ModSettings_QEverything.maxShellQuality.ToString();
            Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelShell2, ref ModSettings_QEverything.maxShellQuality, ref maxShellBuffer, 1, 2, 6);
            listing.End();
        }

        public static void DoSkillsAndInspiration(Rect rect)
        {
            Rect firstCol = new Rect(5f, 110f, rect.width * .35f, rect.height);
            listing.Begin(firstCol);
            listing.CheckboxLabeled("QEverything.Materials".Translate(), ref ModSettings_QEverything.useMaterialQuality);
            listing.CheckboxLabeled("QEverything.Tables".Translate(), ref ModSettings_QEverything.useTableQuality);
            listing.CheckboxLabeled("QEverything.SkillReq".Translate(), ref ModSettings_QEverything.useSkillReq);
            listing.GapLine();

            listing.CheckboxLabeled("QEverything.SkilledAnimals".Translate(), ref ModSettings_QEverything.skilledAnimals);
            listing.CheckboxLabeled("QEverything.SkilledButchering".Translate(), ref ModSettings_QEverything.skilledButchering);
            listing.CheckboxLabeled("QEverything.SkilledHarvesting".Translate(), ref ModSettings_QEverything.skilledHarvesting);
            listing.CheckboxLabeled("QEverything.SkilledMining".Translate(), ref ModSettings_QEverything.skilledMining);
            listing.CheckboxLabeled("QEverything.SkilledStoneCutting".Translate(), ref ModSettings_QEverything.skilledStoneCutting);
            listing.End();

            //Column Two
            Rect secondCol = new Rect(325f, 110f, rect.width * .3f, rect.height);
            listing.Begin(secondCol);
            if (ModSettings_QEverything.useMaterialQuality || ModSettings_QEverything.useTableQuality)
            {
                listing.Gap(12f);
                string labelStd = "QEverything.Standard".Translate() + ((QualityCategory)ModSettings_QEverything.stdSupplyQuality).ToString();
                string stdBuffer = ModSettings_QEverything.stdSupplyQuality.ToString();
                Mod_SettingsUtility.LabeledIntEntry(listing.GetRect(24f), labelStd, ref ModSettings_QEverything.stdSupplyQuality, ref stdBuffer, 1, 0, 6);
                listing.CheckboxLabeled("QEverything.SupplyQuality".Translate(), ref ModSettings_QEverything.multSupplyFactor);
                string awfulSupplyFactor = ModSettings_QEverything.awfulSupplyFactor.ToString();
                Mod_SettingsUtility.LabeledFloatEntry(listing.GetRect(24f), "QEverything.Awful".Translate(), ref ModSettings_QEverything.awfulSupplyFactor, ref awfulSupplyFactor, .05f, .5f, .05f, 10f);
                string poorSupplyFactor = ModSettings_QEverything.poorSupplyFactor.ToString();
                Mod_SettingsUtility.LabeledFloatEntry(listing.GetRect(24f), "QEverything.Poor".Translate(), ref ModSettings_QEverything.poorSupplyFactor, ref poorSupplyFactor, .05f, .5f, .05f, 10f);
                string normSupplyFactor = ModSettings_QEverything.normalSupplyFactor.ToString();
                Mod_SettingsUtility.LabeledFloatEntry(listing.GetRect(24f), "QEverything.Normal".Translate(), ref ModSettings_QEverything.normalSupplyFactor, ref normSupplyFactor, .05f, .5f, .05f, 10f);
                string goodSupplyFactor = ModSettings_QEverything.goodSupplyFactor.ToString();
                Mod_SettingsUtility.LabeledFloatEntry(listing.GetRect(24f), "QEverything.Good".Translate(), ref ModSettings_QEverything.goodSupplyFactor, ref goodSupplyFactor, .05f, .5f, .05f, 10f);
                string excSupplyFactor = ModSettings_QEverything.excSupplyFactor.ToString();
                Mod_SettingsUtility.LabeledFloatEntry(listing.GetRect(24f), "QEverything.Excellent".Translate(), ref ModSettings_QEverything.excSupplyFactor, ref excSupplyFactor, .05f, .5f, .05f, 10f);
                string masterSupplyFactor = ModSettings_QEverything.masterSupplyFactor.ToString();
                Mod_SettingsUtility.LabeledFloatEntry(listing.GetRect(24f), "QEverything.Masterwork".Translate(), ref ModSettings_QEverything.masterSupplyFactor, ref masterSupplyFactor, .05f, .5f, .05f, 10f);
                string legSupplyFactor = ModSettings_QEverything.legSupplyFactor.ToString();
                Mod_SettingsUtility.LabeledFloatEntry(listing.GetRect(24f), "QEverything.Legendary".Translate(), ref ModSettings_QEverything.legSupplyFactor, ref legSupplyFactor, .05f, .5f, .05f, 10f);
                listing.Gap(12f);
            }
            else listing.Gap(64);
            listing.GapLine();

            if (ModSettings_QEverything.stuffQuality)
            {
                if (ModSettings_QEverything.skilledAnimals)
                    listing.CheckboxLabeled("QEverything.InspiredGathering".Translate(), ref ModSettings_QEverything.inspiredGathering);
                else listing.Gap(24f);
                if (ModSettings_QEverything.skilledButchering)
                    listing.CheckboxLabeled("QEverything.InspiredButchering".Translate(), ref ModSettings_QEverything.inspiredButchering);
                else listing.Gap(24f);
                if (ModSettings_QEverything.skilledHarvesting)
                    listing.CheckboxLabeled("QEverything.InspiredHarvesting".Translate(), ref ModSettings_QEverything.inspiredHarvesting);
                else listing.Gap(24f);
                if (ModSettings_QEverything.skilledMining)
                    listing.CheckboxLabeled("QEverything.InspiredMining".Translate(), ref ModSettings_QEverything.inspiredMining);
                else listing.Gap(24f);
                if (ModSettings_QEverything.skilledStoneCutting)
                    listing.CheckboxLabeled("QEverything.InspiredStonecutting".Translate(), ref ModSettings_QEverything.inspiredStonecutting);
                else listing.Gap(24f);
            }
            listing.End();

            //Column Three
            Rect thirdCol = new Rect(600f, 110f, rect.width * .3f, rect.height);
            listing.Begin(thirdCol);
            if (ModSettings_QEverything.useMaterialQuality || ModSettings_QEverything.useTableQuality)
            {
                listing.Gap(15);
                string midLabel = (1 - ModSettings_QEverything.tableFactor).ToStringPercent() + " / " + ModSettings_QEverything.tableFactor.ToStringPercent();
                ModSettings_QEverything.tableFactor = Widgets.HorizontalSlider(listing.GetRect(23f), ModSettings_QEverything.tableFactor, 0, 1f, false, midLabel, "Materials", "Work Table");
                listing.Gap(10);
            }
            else listing.Gap(48);
            listing.Gap(24f);
            listing.GapLine();
            listing.CheckboxLabeled("QEverything.InspiredChemistry".Translate(), ref ModSettings_QEverything.inspiredChemistry);
            listing.CheckboxLabeled("QEverything.InspiredConstruction".Translate(), ref ModSettings_QEverything.inspiredConstruction);
            listing.CheckboxLabeled("QEverything.InspiredCooking".Translate(), ref ModSettings_QEverything.inspiredCooking);
            listing.End();
        }

        public static void DoCustomizeStuff(Rect rect)
        {
            DrawEnableAndBulkRow(rect, "QEverything.Resources".Translate(), ref ModSettings_QEverything.indivStuff,
                Mod_SettingsUtility.PopulateStuff, ModSettings_QEverything.stuffDict);

            listing.Begin(new Rect(0f, 140f, rect.width, 10f));
            listing.GapLine();
            listing.End();

            if (!ModSettings_QEverything.indivStuff) return;

            DrawSearchBar(rect, ref stuffSearch);
            DrawColumnHeaders(rect, "QEverything.Textiles".Translate(), "QEverything.OtherRes".Translate());

            stuffCache.EnsureBuilt(
                ModSettings_QEverything.stuffDict,
                stuffSearch,
                IsStuffTextile);

            DrawPartitionedScrollLists(rect, stuffCache, ModSettings_QEverything.stuffDict,
                ref texScroll, ref resScroll);
        }

        private static bool IsStuffTextile(ThingDef def)
        {
            return def.IsLeather || def.IsWithinCategory(ThingCategoryDefOf.Textiles);
        }

        public static void DoCustomizeBuildings(Rect rect)
        {
            DrawEnableAndBulkRow(rect, "QEverything.Buildings".Translate(), ref ModSettings_QEverything.indivBuildings,
                Mod_SettingsUtility.PopulateBuildings, ModSettings_QEverything.bldgDict);

            listing.Begin(new Rect(0f, 140f, rect.width, 10f));
            listing.GapLine();
            listing.End();

            if (!ModSettings_QEverything.indivBuildings) return;

            DrawSearchBar(rect, ref bldgSearch);
            DrawColumnHeaders(rect, "QEverything.Furniture".Translate(), "QEverything.Power".Translate());

            bldgCache.EnsureBuilt(
                ModSettings_QEverything.bldgDict,
                bldgSearch,
                IsBuildingFurnitureOrProduction);

            DrawPartitionedScrollLists(rect, bldgCache, ModSettings_QEverything.bldgDict,
                ref bldgScroll, ref bldgScroll2);
        }

        private static bool IsBuildingFurnitureOrProduction(ThingDef def)
        {
            return QualityDefUtility.IsBuildingFurnitureOrProduction(def);
        }

        public static void DoCustomizeWeapons(Rect rect)
        {
            DrawStackedColumnButtons(
                new Rect(5f, 110f, rect.width * .48f, rect.height),
                "QEverything.WeapShells".Translate(),
                ref ModSettings_QEverything.indivWeapons,
                Mod_SettingsUtility.PopulateWeapons,
                ModSettings_QEverything.weapDict);

            DrawStackedColumnButtons(
                new Rect(rect.width * .5f, 110f, rect.width * .48f, rect.height),
                "QEverything.Clothing".Translate(),
                ref ModSettings_QEverything.indivApparel,
                Mod_SettingsUtility.PopulateApparel,
                ModSettings_QEverything.appDict);

            if (!ModSettings_QEverything.indivWeapons && !ModSettings_QEverything.indivApparel) return;

            // Shared search bar across both columns (single search per tab).
            SearchableListUI.DrawSearchBar(
                new Rect(5f, 222f, rect.width - 10f, SearchableListUI.SearchBarHeight),
                ref weapAppSearch);

            float scrollY = 255f;
            float scrollHeight = rect.height - 145f;

            if (ModSettings_QEverything.indivWeapons)
            {
                weapCache.EnsureBuilt(ModSettings_QEverything.weapDict, weapAppSearch, null);
                SearchableListUI.DrawVirtualizedCheckboxList(
                    new Rect(5f, scrollY, rect.width * .48f, scrollHeight),
                    ref weapScroll,
                    weapCache.FilteredLeft,
                    ModSettings_QEverything.weapDict);
            }

            if (ModSettings_QEverything.indivApparel)
            {
                appCache.EnsureBuilt(ModSettings_QEverything.appDict, weapAppSearch, null);
                SearchableListUI.DrawVirtualizedCheckboxList(
                    new Rect(rect.width * .5f, scrollY, rect.width * .48f, scrollHeight),
                    ref appScroll,
                    appCache.FilteredLeft,
                    ModSettings_QEverything.appDict);
            }
        }

        public static void DoCustomizeOther(Rect rect)
        {
            // Tab 5 diverges slightly: original had a single "Enable/Disable"
            // button at column 1 with no Select/Deselect buttons at the top.
            // Keep that behavior — bulk toggles aren't offered here, so we only
            // draw the enable button.
            listing.Begin(new Rect(5f, 110f, rect.width * .33f - 5f, 30f));
            if (!ModSettings_QEverything.indivOther)
            {
                if (listing.ButtonText("QEverything.Enable".Translate(), null))
                {
                    Mod_SettingsUtility.PopulateOther();
                    ModSettings_QEverything.indivOther = true;
                }
            }
            else if (listing.ButtonText("QEverything.Disable".Translate(), null))
            {
                ModSettings_QEverything.indivOther = false;
            }
            listing.End();

            listing.Begin(new Rect(0f, 140f, rect.width, 10f));
            listing.GapLine();
            listing.End();

            if (!ModSettings_QEverything.indivOther) return;

            DrawSearchBar(rect, ref otherSearch);
            DrawColumnHeaders(rect, "QEverything.Food".Translate(), "QEverything.OtherFood".Translate());

            otherCache.EnsureBuilt(
                ModSettings_QEverything.otherDict,
                otherSearch,
                IsFoodPartition);

            DrawPartitionedScrollLists(rect, otherCache, ModSettings_QEverything.otherDict,
                ref foodScroll, ref otherScroll);
        }

        private static bool IsFoodPartition(ThingDef def)
        {
            return QualityDefUtility.IsFoodPartition(def);
        }

        // Shared layout helpers — used by the partitioned customization tabs
        // (Stuff, Buildings, Other). Y-coordinates are absolute within the
        // settings window, matching the original layout intent.

        private const float SearchBarY = 150f;
        private const float ColumnHeaderY = 183f;
        private const float ScrollY = 223f;
        private const float ScrollBottomReserve = 143f;

        private static void DrawEnableAndBulkRow(
            Rect rect,
            string label,
            ref bool indivFlag,
            Action populate,
            Dictionary<string, bool> dict)
        {
            listing.Begin(new Rect(5f, 110f, rect.width * .33f - 5f, 30f));
            if (!indivFlag)
            {
                if (listing.ButtonTextLabeled(label, "QEverything.Enable".Translate()))
                {
                    populate();
                    indivFlag = true;
                }
            }
            else if (listing.ButtonTextLabeled(label, "QEverything.Disable".Translate()))
            {
                indivFlag = false;
            }
            listing.End();

            listing.Begin(new Rect(rect.width * .33f + 5f, 110f, rect.width * .33f - 5f, 30f));
            if (listing.ButtonText("QEverything.Select".Translate(), null))
            {
                SetAllValues(dict, true);
            }
            listing.End();

            listing.Begin(new Rect(rect.width * .66f + 5f, 110f, rect.width * .33f - 5f, 30f));
            if (listing.ButtonText("QEverything.Deselect".Translate(), null))
            {
                SetAllValues(dict, false);
            }
            listing.End();
        }

        private static void DrawStackedColumnButtons(
            Rect colRect,
            string label,
            ref bool indivFlag,
            Action populate,
            Dictionary<string, bool> dict)
        {
            listing.Begin(colRect);
            if (!indivFlag)
            {
                if (listing.ButtonTextLabeled(label, "QEverything.Enable".Translate()))
                {
                    populate();
                    indivFlag = true;
                }
            }
            else if (listing.ButtonTextLabeled(label, "QEverything.Disable".Translate()))
            {
                indivFlag = false;
            }

            if (listing.ButtonText("QEverything.Select".Translate(), null))
            {
                SetAllValues(dict, true);
            }
            if (listing.ButtonText("QEverything.Deselect".Translate(), null))
            {
                SetAllValues(dict, false);
            }
            listing.GapLine();
            listing.End();
        }

        private static void DrawSearchBar(Rect rect, ref string searchText)
        {
            SearchableListUI.DrawSearchBar(
                new Rect(5f, SearchBarY, rect.width - 10f, SearchableListUI.SearchBarHeight),
                ref searchText);
        }

        private static void DrawColumnHeaders(Rect rect, string leftHeader, string rightHeader)
        {
            listing.Begin(new Rect(5f, ColumnHeaderY, rect.width * .5f - 10f, 38f));
            listing.Label(leftHeader);
            listing.GapLine();
            listing.End();

            listing.Begin(new Rect(rect.width * .5f + 5f, ColumnHeaderY, rect.width * .5f - 10f, 38f));
            listing.Label(rightHeader);
            listing.GapLine();
            listing.End();
        }

        private static void DrawPartitionedScrollLists(
            Rect rect,
            QualityListCache cache,
            Dictionary<string, bool> dict,
            ref Vector2 leftScroll,
            ref Vector2 rightScroll)
        {
            float scrollHeight = rect.height - ScrollBottomReserve;

            SearchableListUI.DrawVirtualizedCheckboxList(
                new Rect(5f, ScrollY, rect.width * .5f - 10f, scrollHeight),
                ref leftScroll,
                cache.FilteredLeft,
                dict);

            SearchableListUI.DrawVirtualizedCheckboxList(
                new Rect(rect.width * .5f + 5f, ScrollY, rect.width * .5f - 10f, scrollHeight),
                ref rightScroll,
                cache.FilteredRight,
                dict);
        }

        // Bulk toggle helper. Takes a snapshot of keys so we aren't mutating
        // the dict's value set while the enumerator is alive (safe per spec,
        // but belt-and-suspenders).
        private static void SetAllValues(Dictionary<string, bool> dict, bool value)
        {
            if (dict.Count == 0) return;
            string[] keys = new string[dict.Count];
            dict.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                dict[keys[i]] = value;
            }
        }
    }
}

