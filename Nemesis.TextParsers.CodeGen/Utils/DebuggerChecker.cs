﻿using System.Diagnostics;

namespace Nemesis.TextParsers.CodeGen.Utils;

static class DebuggerChecker
{
    [Conditional("DEBUG")]
    public static void CheckDebugger(this GeneratorExecutionContext context, string generatorName)
    {
        bool ShouldDebug(string optionName) =>
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(optionName, out var option) &&
            bool.TryParse(option, out var shouldDebug) && shouldDebug;

        if (ShouldDebug("build_property.DebugSourceGenerators") || ShouldDebug($"build_property.Debug{generatorName}"))
            Debugger.Launch();
    }
}
