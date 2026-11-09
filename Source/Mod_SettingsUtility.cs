namespace ConfigurableQuality
{
    internal static class Mod_SettingsUtility
    {
        public static void RestoreDefaults()
        {
            QualitySettingsState.Editor.Reset();
            QualitySettingsState.Changed();
        }
    }
}
