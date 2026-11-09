using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorldDevTests;

namespace ConfigurableQuality.DevTests;

public sealed class ModHarness : HarnessSettings
{
    public override string DisplayName => "Configurable Quality";
    public override string ArgPrefix => "cq";
    public override string PackageId => "kylemhb.configurablequality";
    // Mod_ConfigurableQuality is internal.
    public override Type ProductionType => AccessTools.TypeByName("ConfigurableQuality.Mod_ConfigurableQuality");
    public override string EvidenceFolder => "ConfigurableQualityTests";
    public override string[] RuntimeFolders => new[] { "About", "Common", "1.6", "1.7" };
    public override string[] RuntimeFiles => new[] { "LoadFolders.xml", "LICENSE", "build-manifest.json" };
    public override IEnumerable<Batch> CreateBatches() => new Batch[] { new FreshBatch(), new ProgressionBatch(), new ProductionBatch(), new MarketBatch() };
    public override IEnumerable<string> RunAll => new[] { "cq-fresh", "cq-progression", "cq-production", "cq-market" };
}
