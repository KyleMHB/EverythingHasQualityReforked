namespace ConfigurableQuality
{
    internal static class Mod_SettingsUtility
    {
        public static void RestoreDefaults()
        {
            var defaults = new QualityConfiguration();
            var draft = QualitySettingsState.Draft;
            draft.ProgressionCompatibility = defaults.ProgressionCompatibility;
            draft.Production = defaults.Production;
            draft.Assignment = defaults.Assignment;
            draft.VanillaRemoval = defaults.VanillaRemoval;
            draft.Participation = defaults.Participation;
            draft.Effects = defaults.Effects;
            draft.MarketValue = defaults.MarketValue;
            draft.LinkedFurnitureBonusCap = defaults.LinkedFurnitureBonusCap;
            draft.LinkedFurniturePenaltyCap = defaults.LinkedFurniturePenaltyCap;
            QualitySettingsState.Changed();
        }
    }
}
