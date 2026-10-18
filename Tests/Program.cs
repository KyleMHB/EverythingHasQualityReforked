using System;
using System.Collections.Generic;
using QualityEverything;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

class Program
{
    static void Main()
    {
        CheckPatchReplacement();
        var scope = new QualityOperationContext<int>(-1);
        var first = scope.Wrap(Values(() => scope.Current, 2), 2).GetEnumerator();
        var second = scope.Wrap(Values(() => scope.Current, 6), 6).GetEnumerator();
        Check(first.MoveNext() && first.Current == 2, "first operation");
        Check(scope.Current == -1, "context does not leak across yields");
        Check(second.MoveNext() && second.Current == 6, "interleaved operation");
        Check(first.MoveNext() && first.Current == 2, "first retains context");
        first.Dispose(); second.Dispose();
        var independent = new QualityOperationContext<int>(-8);
        foreach (var value in scope.Wrap(Values(() => independent.Current, 0), 4)) Check(value == -8, "independent contexts do not share values");
        foreach (var value in scope.Wrap(Nested(scope), 3)) Check(value == 3, "nested context restores outer");
        try { foreach (var value in scope.Wrap(Failing(), 5)) { } } catch (InvalidOperationException) { }
        Check(scope.Current == -1, "exception restores context");
        var disposing = scope.Wrap(DisposeInside(scope), 4).GetEnumerator();
        Check(disposing.MoveNext(), "disposable iterator starts");
        disposing.Dispose();
        Check(scope.Current == -1, "disposal restores context");
        Console.WriteLine("Production context checks passed.");
    }
    public static int Original(int a, int b, bool c) => a + b + (c ? 1 : 0);
    public static int Replacement(int a, int b, bool c, int multiplier) => Original(a, b, c) * multiplier;
    static void CheckPatchReplacement()
    {
        var original = typeof(Program).GetMethod(nameof(Original));
        var replacement = typeof(Program).GetMethod(nameof(Replacement));
        var code = new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(OpCodes.Call, original), new CodeInstruction(OpCodes.Ret) };
        var result = QualityPatchUtility.ReplaceSingleCall(code, original, replacement,
            () => new[] { new CodeInstruction(OpCodes.Ldc_I4, 7) }, "fixture");
        var method = new DynamicMethod("QualityReplacement", typeof(int), new[] { typeof(int), typeof(int), typeof(bool) }, typeof(Program), true);
        var il = method.GetILGenerator();
        foreach (var instruction in result)
        {
            if (instruction.operand is MethodInfo target) il.Emit(instruction.opcode, target);
            else if (instruction.operand is int number) il.Emit(instruction.opcode, number);
            else il.Emit(instruction.opcode);
        }
        var run = (Func<int, int, bool, int>)method.CreateDelegate(typeof(Func<int, int, bool, int>));
        Check(run(2, 3, true) == 42, "replacement preserves original arguments and consumes extra input");
        var mismatch = QualityPatchUtility.ReplaceSingleCall(new[] { code[3], code[3] }, original, replacement,
            () => throw new Exception("must not partially transform mismatch"), "fixture mismatch");
        Check(mismatch.Count == 2 && mismatch[0].Calls(original) && mismatch[1].Calls(original), "ambiguous match leaves original intact");
        Check(Verse.Log.Errors == 1, "one diagnostic for unmatched feature");
    }    static IEnumerable<int> Values(Func<int> current, int unused) { yield return current(); yield return current(); }
    static IEnumerable<int> Nested(QualityOperationContext<int> scope)
    {
        foreach (var inner in scope.Wrap(Values(() => scope.Current, 6), 6)) Check(inner == 6, "nested operation");
        yield return scope.Current;
    }
    static IEnumerable<int> DisposeInside(QualityOperationContext<int> scope)
    {
        try { yield return scope.Current; }
        finally { Check(scope.Current == 4, "dispose executes inside own operation"); }
    }    static IEnumerable<int> Failing() { yield return Throw(); }
    static int Throw() { throw new InvalidOperationException(); }
    static void Check(bool value, string name) { if (!value) throw new Exception(name); }
}
namespace Verse
{
    static class Log { public static int Errors; public static void Error(string message) { Errors++; } }
}
