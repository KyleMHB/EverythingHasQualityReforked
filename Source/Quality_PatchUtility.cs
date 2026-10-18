using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace QualityEverything
{
    internal static class QualityPatchUtility
    {
        private static readonly Dictionary<string, bool> statuses = new Dictionary<string, bool>(StringComparer.Ordinal);
        public static IReadOnlyDictionary<string, bool> Statuses => statuses;

        public static List<CodeInstruction> ReplaceSingleCall(IEnumerable<CodeInstruction> instructions,
            MethodInfo original, MethodInfo replacement, Func<IEnumerable<CodeInstruction>> extraArguments, string feature)
        {
            var code = instructions.ToList();
            if (original == null || code.Count(i => i.Calls(original)) != 1)
            {
                statuses[feature] = false;
                Log.Error("QEverything: " + feature + " pattern did not match exactly once; leaving original behavior intact.");
                return code;
            }
            var result = new List<CodeInstruction>();
            foreach (var instruction in code)
            {
                if (!instruction.Calls(original)) { result.Add(instruction); continue; }
                var arguments = extraArguments().ToList();
                var call = new CodeInstruction(OpCodes.Call, replacement);
                var first = arguments.Count == 0 ? call : arguments[0];
                first.labels.AddRange(instruction.labels);
                // A protected-region boundary at the call must also cover its new arguments.
                first.blocks.AddRange(instruction.blocks.Where(b => b.blockType != ExceptionBlockType.EndExceptionBlock));
                call.blocks.AddRange(instruction.blocks.Where(b => b.blockType == ExceptionBlockType.EndExceptionBlock));
                result.AddRange(arguments);
                result.Add(call);
            }
            statuses[feature] = true;
            return result;
        }
    }
}
