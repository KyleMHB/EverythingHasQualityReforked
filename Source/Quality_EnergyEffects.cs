using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ConfigurableQuality
{
    // One adapter owns every native battery capacity/efficiency consumer. Shared Props remain untouched.
    internal static class QualityEnergyEffects
    {
        private static bool registered;
        private static readonly HashSet<ThingDef> activeCapacity = new HashSet<ThingDef>();
        private static readonly HashSet<ThingDef> activeEfficiency = new HashSet<ThingDef>();
        private static readonly FieldInfo maximum = AccessTools.Field(typeof(CompProperties_Battery), "storedEnergyMax");
        private static readonly FieldInfo efficiency = AccessTools.Field(typeof(CompProperties_Battery), "efficiency");
        private static readonly MethodInfo consumption = AccessTools.PropertyGetter(typeof(CompProperties_Power), "PowerConsumption");
        private static readonly MethodInfo draw = AccessTools.Method(typeof(Building_Battery), "DrawAt");
        private static readonly MethodInfo expose = AccessTools.Method(typeof(CompPowerBattery), "PostExposeData");
        private static readonly string[] batteryMethods = { "get_AmountCanAccept", "get_StoredEnergyPct", "SetStoredEnergyPct", "AddEnergy", "PostExposeData", "CompInspectStringExtra" };

        public static void Register()
        {
            if (registered) return;
            registered = true;
            var harmony = new Harmony("kylemhb.configurablequality.energy");
            bool power = false, battery = false;
            string powerReason = "QEverything.Energy.PowerAvailable".Translate().ToString();
            string batteryReason = "QEverything.Energy.BatteryAvailable".Translate().ToString();
            try
            {
                var inspect = AccessTools.Method(typeof(CompPowerTrader), "CompInspectStringExtra");
                if (PatchProcessor.GetOriginalInstructions(inspect).Count(i => i.Calls(consumption)) != 1)
                    throw new InvalidOperationException("Native active-demand inspection call did not match.");
                harmony.Patch(inspect, transpiler: Method("PowerInspect"));
                harmony.Patch(AccessTools.PropertyGetter(typeof(CompPowerTrader), "PowerOutput"), postfix: Method("Output"));
                power = true;
            }
            catch (Exception error) { powerReason = error.Message; Log.Warning("QEverything power effects unavailable: " + error.Message); }
            try
            {
                // Preflight the whole set before patching: partial capacity accounting is unsafe.
                int[] maxCounts = { 1, 1, 1, 0, 2, 1 };
                int[] efficiencyCounts = { 1, 0, 0, 1, 0, 1 };
                for (int n = 0; n < batteryMethods.Length; n++)
                    Verify(AccessTools.Method(typeof(CompPowerBattery), batteryMethods[n]), maxCounts[n], efficiencyCounts[n]);
                Verify(draw, 1, 0);
                foreach (string name in batteryMethods)
                    harmony.Patch(AccessTools.Method(typeof(CompPowerBattery), name), transpiler: Method("BatteryFields"));
                harmony.Patch(draw, transpiler: Method("BatteryFields"));
                foreach (string name in new[] { "get_AmountCanAccept", "get_StoredEnergyPct", "get_StoredEnergy", "AddEnergy", "CompInspectStringExtra" })
                    harmony.Patch(AccessTools.Method(typeof(CompPowerBattery), name), prefix: Method("BeforeBattery"));
                harmony.Patch(AccessTools.PropertyGetter(typeof(CompPowerBattery), "AmountCanAccept"), postfix: Method("Acceptance"));
                harmony.Patch(AccessTools.Method(typeof(CompQuality), "SetQuality"), postfix: Method("QualityChanged"));
                battery = true;
            }
            catch (Exception error) { batteryReason = error.Message; Log.Warning("QEverything battery effects unavailable: " + error.Message); }
            bool otherGeneration = AccessTools.TypeByName("QualityPower.Patches.Patch_CompPowerTrader_PowerOutput") != null;
            bool otherBattery = AccessTools.TypeByName("QualityPower.Patches.Patch_CompPowerBattery") != null;
            QualityEffects.SetCapability(EffectId.GeneratorOutput, power && !otherGeneration, otherGeneration ? "QEverything.Energy.OtherGeneration".Translate().ToString() : powerReason);
            QualityEffects.SetCapability(EffectId.ConsumerDemand, power, powerReason);
            QualityEffects.SetCapability(EffectId.BatteryCapacity, battery && !otherBattery, otherBattery ? "QEverything.Energy.OtherCapacity".Translate().ToString() : batteryReason);
            QualityEffects.SetCapability(EffectId.BatteryEfficiency, battery && !otherBattery, otherBattery ? "QEverything.Energy.OtherEfficiency".Translate().ToString() : batteryReason);
            ScopeDefinitions();
            var stat = DefDatabase<StatDef>.GetNamedSilentFail("BatteryCapacity");
            if (stat != null && stat.Worker.GetType().FullName == "BatteryCapacity.StatWorker_BatteryCapacity")
            {
                stat.parts = stat.parts ?? new List<StatPart>();
                if (!stat.parts.Any(p => p is StatPart_QualityBatteryCapacity))
                    stat.parts.Add(new StatPart_QualityBatteryCapacity { parentStat = stat });
            }
        }

        private static HarmonyMethod Method(string name) => new HarmonyMethod(typeof(QualityEnergyEffects), name);
        private static void Verify(MethodInfo method, int capacityCount, int efficiencyCount)
        {
            if (method == null) throw new InvalidOperationException("Missing native battery consumer.");
            VerifyFields(PatchProcessor.GetOriginalInstructions(method), method.Name, capacityCount, efficiencyCount);
        }

        private static void VerifyFields(IEnumerable<CodeInstruction> instructions, string methodName, int capacityCount, int efficiencyCount)
        {
            var code = instructions.ToList();
            if (code.Count(i => Equals(i.operand, maximum) && (i.opcode == OpCodes.Ldfld || i.opcode == OpCodes.Ldflda)) != capacityCount ||
                code.Count(i => Equals(i.operand, efficiency) && i.opcode == OpCodes.Ldfld) != efficiencyCount)
                throw new InvalidOperationException("Native battery field pattern changed: " + methodName);
        }

        private static void ScopeDefinitions()
        {
            bool capacityEnabled = QualityEffects.Available(EffectId.BatteryCapacity) && QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(nameof(EffectId.BatteryCapacity), out var capacity) && capacity.Enabled;
            bool efficiencyEnabled = QualityEffects.Available(EffectId.BatteryEfficiency) && QualitySettingsState.ActiveSnapshot.Effects.TryGetValue(nameof(EffectId.BatteryEfficiency), out var charging) && charging.Enabled;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var props = def.GetCompProperties<CompProperties_Battery>();
                if (props == null) continue;
                string custom = props.compClass != typeof(CompPowerBattery) ? "QEverything.Energy.CustomComponent".Translate().ToString() : null;
                string capacityReason = custom;
                if (!QualityEnergyCalculation.Finite(props.storedEnergyMax) || props.storedEnergyMax <= 0f)
                    capacityReason = "QEverything.Energy.InvalidCapacity".Translate().ToString();
                if (def.thingClass == null || !typeof(Building_Battery).IsAssignableFrom(def.thingClass) || AccessTools.Method(def.thingClass, "DrawAt")?.DeclaringType != typeof(Building_Battery))
                    capacityReason = "QEverything.Energy.CustomRenderer".Translate().ToString();
                QualityEffects.SetUnsupported(EffectId.BatteryCapacity, def, capacityReason);
                string efficiencyReason = custom ?? (!QualityEnergyCalculation.Finite(props.efficiency) || props.efficiency <= 0f || props.efficiency > 1f ? "QEverything.Energy.InvalidEfficiency".Translate().ToString() : null);
                QualityEffects.SetUnsupported(EffectId.BatteryEfficiency, def, efficiencyReason);
                // Active options and participation are immutable until restart. Resolve eligibility
                // once so disabled/excluded batteries retain native reads and quality hooks stay cheap.
                if (!QualityCatalog.Participates(def) || !def.comps.Any(p => p?.compClass != null && typeof(CompQuality).IsAssignableFrom(p.compClass))) continue;
                if (capacityEnabled && capacityReason == null) activeCapacity.Add(def);
                if (efficiencyEnabled && efficiencyReason == null) activeEfficiency.Add(def);
            }
        }

        private static void Output(CompPowerTrader __instance, ref float __result)
        {
            if (__result == 0f) return;
            __result *= QualityEffects.Factor(__result > 0f ? EffectId.GeneratorOutput : EffectId.ConsumerDemand, __instance.parent);
        }
        private static float ActiveDemand(CompProperties_Power props, CompPowerTrader comp)
        {
            float current = props.PowerConsumption;
            return current * QualityEffects.Factor(current < 0f ? EffectId.GeneratorOutput : EffectId.ConsumerDemand, comp.parent);
        }
        private static IEnumerable<CodeInstruction> PowerInspect(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            if (code.Count(i => i.Calls(consumption)) != 1) throw new InvalidOperationException("Active-demand consumer changed by another transpiler.");
            return QualityPatchUtility.ReplaceSingleCall(code, consumption, AccessTools.Method(typeof(QualityEnergyEffects), "ActiveDemand"), () => new[] { new CodeInstruction(OpCodes.Ldarg_0) }, "power active demand display");
        }

        private static bool LoadingEarly => Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs;
        private static float Maximum(CompProperties_Battery props, CompPowerBattery comp)
        {
            // Quality may be a later sibling comp during LoadingVars. Native storedPower is absolute;
            // the native clamp must wait until PostLoadInit, including for held/minified batteries.
            if (LoadingEarly && activeCapacity.Contains(comp.parent.def))
                return float.MaxValue;
            return EffectiveMaximum(props, comp);
        }
        internal static float EffectiveMaximum(CompProperties_Battery props, CompPowerBattery comp)
        {
            if (!activeCapacity.Contains(comp.parent.def)) return props.storedEnergyMax;
            float value = props.storedEnergyMax * QualityEffects.Factor(EffectId.BatteryCapacity, comp.parent);
            return QualityEnergyCalculation.Finite(value) && value > 0f ? value : props.storedEnergyMax;
        }
        private static float Efficiency(CompProperties_Battery props, CompPowerBattery comp)
        {
            if (!activeEfficiency.Contains(comp.parent.def)) return props.efficiency;
            float factor = QualityEffects.Factor(EffectId.BatteryEfficiency, comp.parent);
            return factor == 1f ? props.efficiency : QualityEnergyCalculation.Efficiency(props.efficiency, factor);
        }
        private static float BuildingMaximum(CompProperties_Battery props, ThingWithComps building) => activeCapacity.Contains(building.def) ? EffectiveMaximum(props, building.GetComp<CompPowerBattery>()) : props.storedEnergyMax;
        private static void BeforeBattery(CompPowerBattery __instance) => Reconcile(__instance);
        private static void Reconcile(CompPowerBattery comp)
        {
            if (comp == null || !activeCapacity.Contains(comp.parent.def) || LoadingEarly) return;
            float maximumValue = EffectiveMaximum(comp.Props, comp);
            // DrawPower takes absolute energy and cannot increase storedPower. No percentage setter.
            // StoredEnergy is patched too, so use a guarded field reader for the reconciliation itself.
            float stored = storedEnergy(comp);
            float retained = QualityEnergyCalculation.ClampStored(stored, maximumValue);
            if (stored > retained) comp.DrawPower(stored - retained);
        }
        private static readonly AccessTools.FieldRef<CompPowerBattery, float> storedEnergy = AccessTools.FieldRefAccess<CompPowerBattery, float>("storedEnergy");
        private static void QualityChanged(CompQuality __instance)
        {
            if (activeCapacity.Contains(__instance.parent.def)) Reconcile(__instance.parent.GetComp<CompPowerBattery>());
        }
        private static void Acceptance(CompPowerBattery __instance, ref float __result)
        {
            if (!activeCapacity.Contains(__instance.parent.def) && !activeEfficiency.Contains(__instance.parent.def)) return;
            // Native early broken/EMP zeros must survive. Only the active supported effect's
            // arithmetic needs the nonnegative/zero-efficiency correction.
            if (QualityEffects.Factor(EffectId.BatteryCapacity, __instance.parent) == 1f && QualityEffects.Factor(EffectId.BatteryEfficiency, __instance.parent) == 1f) return;
            if (__instance.parent.IsBrokenDown() || __instance.StunnedByEMP) { __result = 0f; return; }
            __result = QualityEnergyCalculation.Acceptance(EffectiveMaximum(__instance.Props, __instance), storedEnergy(__instance), Efficiency(__instance.Props, __instance));
        }

        private static IEnumerable<CodeInstruction> BatteryFields(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
        {
            var code = instructions.ToList();
            string methodName = __originalMethod.Name;
            int capacityCount = methodName == "AddEnergy" ? 0 : methodName == "PostExposeData" ? 2 : 1;
            int efficiencyCount = methodName == "get_AmountCanAccept" || methodName == "AddEnergy" || methodName == "CompInspectStringExtra" ? 1 : 0;
            VerifyFields(code, methodName, capacityCount, efficiencyCount);
            foreach (var instruction in code)
            {
                bool isMaximum = Equals(instruction.operand, maximum);
                if ((!isMaximum && !Equals(instruction.operand, efficiency)) || (instruction.opcode != OpCodes.Ldfld && instruction.opcode != OpCodes.Ldflda))
                { yield return instruction; continue; }
                var context = new CodeInstruction(OpCodes.Ldarg_0);
                context.labels.AddRange(instruction.labels);
                context.blocks.AddRange(instruction.blocks.Where(b => b.blockType != ExceptionBlockType.EndExceptionBlock));
                yield return context;
                string helper = !isMaximum ? "Efficiency" : __originalMethod.DeclaringType == typeof(Building_Battery) ? "BuildingMaximum" : Equals(__originalMethod, expose) ? "Maximum" : "EffectiveMaximum";
                var call = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QualityEnergyEffects), helper));
                call.blocks.AddRange(instruction.blocks.Where(b => b.blockType == ExceptionBlockType.EndExceptionBlock));
                yield return call;
                if (instruction.opcode == OpCodes.Ldflda)
                {
                    // Inspection formats an address. Give it an instance-derived local, never a
                    // reference into the shared CompProperties_Battery definition.
                    var local = generator.DeclareLocal(typeof(float));
                    yield return new CodeInstruction(OpCodes.Stloc, local);
                    yield return new CodeInstruction(OpCodes.Ldloca, local);
                }
            }
        }
    }

    internal sealed class StatPart_QualityBatteryCapacity : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing) val *= QualityEffects.Factor(EffectId.BatteryCapacity, req.Thing);
        }
        public override string ExplanationPart(StatRequest req)
        {
            float factor = req.HasThing ? QualityEffects.Factor(EffectId.BatteryCapacity, req.Thing) : 1f;
            return factor == 1f ? null : "QEverything.Options.Effect.BatteryCapacity".Translate() + ": x" + factor.ToString("0.##");
        }
    }
}
