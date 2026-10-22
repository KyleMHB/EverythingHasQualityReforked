using System.Globalization;

namespace QualityEverything
{
    internal readonly struct QualityNumericRange
    {
        private readonly float minimum, maximum;
        public QualityNumericRange(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }
        public bool Contains(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;
        public bool TryParse(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && Contains(value);
        public string Hint => minimum.ToString("0.###", CultureInfo.InvariantCulture) + "-" + maximum.ToString("0.###", CultureInfo.InvariantCulture);

        public static readonly QualityNumericRange Effect = new QualityNumericRange(.01f, 10f);
        public static readonly QualityNumericRange LinkedBonus = new QualityNumericRange(0f, .02f);
        public static readonly QualityNumericRange LinkedPenalty = new QualityNumericRange(0f, .005f);
        public static QualityNumericRange Production(string field) => new QualityNumericRange(0f, field == "tableFactor" ? 1f : 10f);
        public static QualityNumericRange ForInput(string key)
        {
            if (key.StartsWith("effect:", System.StringComparison.Ordinal)) return Effect;
            if (key == "linkedBonus") return LinkedBonus;
            if (key == "linkedPenalty") return LinkedPenalty;
            return Production(key.Substring("production:".Length));
        }
    }
}
