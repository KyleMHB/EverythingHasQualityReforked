using System;

namespace ConfigurableQuality
{
    // At most one contribution from each verified native furniture kind.
    internal struct QualityFurnitureContribution
    {
        private float? dresser, endTable;
        public void Offer(int kind, float factor)
        {
            if (float.IsNaN(factor) || float.IsInfinity(factor)) return;
            if (kind == 0) dresser = dresser.HasValue ? Math.Max(dresser.Value, factor) : factor;
            else if (kind == 1) endTable = endTable.HasValue ? Math.Max(endTable.Value, factor) : factor;
        }
        public float Delta(float bonusCap, float penaltyCap)
        {
            float value = ((dresser ?? 1f) - 1f + (endTable ?? 1f) - 1f) * .05f;
            return Math.Max(-Math.Min(.005f, Math.Max(0f, penaltyCap)), Math.Min(Math.Min(.02f, Math.Max(0f, bonusCap)), value));
        }
    }
}
