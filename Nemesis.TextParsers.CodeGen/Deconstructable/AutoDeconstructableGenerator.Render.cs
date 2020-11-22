﻿using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Deconstructable
{
    public partial class AutoDeconstructableGenerator
    {
        private static string RenderRecord(INamedTypeSymbol typeSymbol, string typeModifiers, IReadOnlyList<(string Name, string Type)> members, GeneratedDeconstructableSettings? settings, IEnumerable<string> namespaces, in GeneratorExecutionContext context)
        {
            if (!typeSymbol.ContainingSymbol.Equals(typeSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                ReportError(context, DiagnosticsId.NamespaceAndTypeNamesEqual, typeSymbol, $"Type '{typeSymbol.Name}' cannot be equal to containing namespace: '{typeSymbol.ContainingNamespace}'");
                return "";
            } 

            string namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();

            var source = new StringBuilder(512);
            foreach (var ns in namespaces)
                source.Append("using ").Append(ns).AppendLine(";");

            var typeName = typeSymbol.Name;
            
            source.Append($@"
namespace {namespaceName}
{{
    [Transformer(typeof({typeName}Transformer))]
    {typeModifiers} {typeName} {{ }}

    [System.CodeDom.Compiler.GeneratedCode(""AutoDeconstructableGenerator"", ""1.0"")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class {typeName}Transformer : TransformerBase<{typeName}>
    {{");

            if (settings is { } s)
            {
                source.AppendLine($@"
        private readonly TupleHelper _helper = new TupleHelper({Escape(s.Delimiter)}, {Escape(s.NullElementMarker)}, {Escape(s.EscapingSequenceStart)}, {Escape(s.Start)}, {Escape(s.End)});");
            }
            else
            {
                source.Append($@"
        public {typeName}Transformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {{");
                source.Append($@"
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
");
                source.Append(@"
        }
");
            }

            foreach (var (name, type) in members)
                source.AppendLine($@"        private readonly ITransformer<{type}> _transformer_{name} = TextTransformer.Default.GetTransformer<{type}>();");

            source.AppendLine($"        private const int ARITY = {members.Count};").AppendLine();

            RenderParseCore(source, typeName, members);
            source.AppendLine();
            RenderFormat(source, typeName, members);

            source.AppendLine("    }");
            source.AppendLine("}");
            return source.ToString();

            static string Escape(char? c)
            {
                return c switch
                {
                    null => "null",
                    '\'' => GetEscapeCode(@"'"), // single quote, needed for character literals
                    '\"' => GetEscapeCode("\""), // double quote, needed for string literals
                    '\\' => GetEscapeCode(@"\"), // backslash
                    '\0' => GetEscapeCode(@"0"), // Unicode character 0
                    '\a' => GetEscapeCode(@"a"), // Alert (character 7)
                    '\b' => GetEscapeCode(@"b"), // Backspace (character 8)
                    '\f' => GetEscapeCode(@"f"), // Form feed (character 12)
                    '\n' => GetEscapeCode(@"n"), // New line (character 10)
                    '\r' => GetEscapeCode(@"r"), // Carriage return (character 13)
                    '\t' => GetEscapeCode(@"t"), // Horizontal tab (character 9)
                    '\v' => GetEscapeCode(@"v"), // Vertical tab (character 11)
                    _ => $"'{c}'"
                };
                static string GetEscapeCode(string ch) => $@"'\{ch}'";
            }
        }
        
        private static void RenderParseCore(StringBuilder source, string typeName, IReadOnlyList<(string Name, string Type)> members)
        {
            source.AppendLine($"        protected override {typeName} ParseCore(in ReadOnlySpan<char> input)");
            source.AppendLine("        {");
            source.AppendLine("            var enumerator = _helper.ParseStart(input, ARITY);");

            for (int i = 1; i <= members.Count; i++)
            {
                if (i != 1)
                    source.AppendLine($"            _helper.ParseNext(ref enumerator, {i});");
                source.AppendLine($"            var t{i} = _helper.ParseElement(ref enumerator, _transformer_{members[i - 1].Name});").AppendLine();
            }

            source.Append("            _helper.ParseEnd(ref enumerator, ARITY);").AppendLine();
            source.Append($"            return new {typeName}(");
            for (int i = 1; i <= members.Count; i++)
            {
                source.Append($"t{i}");
                if (i < members.Count)
                    source.Append(", ");
            }
            source.AppendLine(");");
            source.AppendLine("        }");
        }

        private static void RenderFormat(StringBuilder source, string typeName, IReadOnlyList<(string Name, string Type)> members)
        {
            source.AppendLine($"        public override string Format({typeName} element)");
            source.AppendLine("        {");
            source.AppendLine("            Span<char> initialBuffer = stackalloc char[32];");
            source.AppendLine("            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);");
            source.AppendLine("            try");
            source.AppendLine("            {");
            source.AppendLine("                 _helper.StartFormat(ref accumulator);");

            source.Append("                 var (");
            for (int i = 0; i < members.Count; i++)
            {
                source.Append($"{members[i].Name}");
                if (i < members.Count - 1)
                    source.Append(", ");
            }
            source.AppendLine(") = element;");

            for (int i = 1; i <= members.Count; i++)
            {
                if (i != 1)
                    source.AppendLine("                _helper.AddDelimiter(ref accumulator);");
                source.AppendLine($"                _helper.FormatElement(_transformer_{members[i - 1].Name}, {members[i - 1].Name}, ref accumulator);").AppendLine();
            }

            source.AppendLine("                _helper.EndFormat(ref accumulator);");
            source.AppendLine("                return accumulator.AsSpan().ToString();");
            source.AppendLine("            }");
            source.AppendLine("            finally { accumulator.Dispose(); }");
            source.AppendLine("        }");
        }
    }
}