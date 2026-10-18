using System;
using QualityEverything;
class FurnitureChecks
{
    static void Equal(float actual, float expected, string name) { if (float.IsNaN(actual) || Math.Abs(actual - expected) > .000001f) throw new Exception(name + ": " + actual); }
    static void Main()
    {
        var normal = new QualityFurnitureContribution(); normal.Offer(0, 1); normal.Offer(1, 1);
        Equal(normal.Delta(.02f, .005f), 0, "Normal is neutral");
        var one = new QualityFurnitureContribution(); one.Offer(0, 1.2f); one.Offer(0, 1.2f); one.Offer(0, .95f);
        Equal(one.Delta(.02f, .005f), .01f, "duplicate kind contributes once using its best active factor");
        one.Offer(1, 1.2f); Equal(one.Delta(.02f, .005f), .02f, "two legendary kinds give two points");
        var awful = new QualityFurnitureContribution(); awful.Offer(0, .95f); awful.Offer(1, .95f);
        Equal(awful.Delta(.02f, .005f), -.005f, "two awful kinds lose half a point");
        var custom = new QualityFurnitureContribution(); custom.Offer(0, 10); custom.Offer(1, 10);
        Equal(custom.Delta(.02f, .005f), .02f, "extreme custom bonus remains capped");
        var penalty = new QualityFurnitureContribution(); penalty.Offer(0, 0); penalty.Offer(1, 0);
        Equal(penalty.Delta(.02f, .005f), -.005f, "extreme custom penalty remains capped");
        Equal(custom.Delta(.004f, .001f), .004f, "configured lower cap wins");
        Equal(penalty.Delta(.02f, 0), 0, "zero penalty cap disables penalty");
        Console.WriteLine("8 linked furniture calculation checks passed.");
    }
}
