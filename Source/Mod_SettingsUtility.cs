namespace QualityEverything
{
    internal static class Mod_SettingsUtility
    {
        public static void RestoreDefaults()
        {
            var defaults = new QualityConfiguration();
            var draft = QualitySettingsState.Draft;
            draft.Production = defaults.Production;
            draft.Assignment = defaults.Assignment;
            draft.Participation = defaults.Participation;
            draft.Effects = defaults.Effects;
            draft.LinkedFurnitureBonusCap = defaults.LinkedFurnitureBonusCap;
            draft.LinkedFurniturePenaltyCap = defaults.LinkedFurniturePenaltyCap;
            QualitySettingsState.Changed();
        }
    }
}
