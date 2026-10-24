using RimWorld;

namespace ConfigurableQuality
{
    // Retain the public fields for existing callers while using one DefOf declaration.
    public static class DefOf_QEverything
    {
        public static InspirationDef QF_Inspired_Butchering;
        public static InspirationDef QF_Inspired_Chemistry;
        public static InspirationDef QF_Inspired_Construction;
        public static InspirationDef QF_Inspired_Cooking;
        public static InspirationDef QF_Inspired_Gathering;
        public static InspirationDef QF_Inspired_Harvesting;
        public static InspirationDef QF_Inspired_Mining;
        public static InspirationDef QF_Inspired_Stonecutting;
        static DefOf_QEverything()
        {
            QF_Inspired_Butchering = DefOf_QFramework.QF_Inspired_Butchering;
            QF_Inspired_Chemistry = DefOf_QFramework.QF_Inspired_Chemistry;
            QF_Inspired_Construction = DefOf_QFramework.QF_Inspired_Construction;
            QF_Inspired_Cooking = DefOf_QFramework.QF_Inspired_Cooking;
            QF_Inspired_Gathering = DefOf_QFramework.QF_Inspired_Gathering;
            QF_Inspired_Harvesting = DefOf_QFramework.QF_Inspired_Harvesting;
            QF_Inspired_Mining = DefOf_QFramework.QF_Inspired_Mining;
            QF_Inspired_Stonecutting = DefOf_QFramework.QF_Inspired_Stonecutting;
        }
    }
}
