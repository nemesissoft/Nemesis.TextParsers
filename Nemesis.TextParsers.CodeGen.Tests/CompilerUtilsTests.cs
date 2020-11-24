using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Nemesis.TextParsers.CodeGen.Utils;

using NUnit.Framework;
#nullable enable

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [TestFixture]
    class CompilerUtilsTests
    {
        private const string NAMESPACE_EXTRACT_CODE = @"
namespace NumberNamespace
{
    readonly struct Number { }
}

namespace CollectionNamespace
{
    class NumberCollection: System.Collections.ObjectModel.Collection<NumberNamespace.Number> { }
}

namespace NullableNamespace
{
    readonly struct MyNullable<T> where T:struct { }
}

namespace TestNamespace
{
    class Test
    {
        public System.Numerics.BigInteger Bi;
        public NumberNamespace.Number N1;
        public NumberNamespace.Number[] A1;
        public CollectionNamespace.NumberCollection Nc1;
        public System.Collections.ObjectModel.Collection<NumberNamespace.Number?> C_N_N;
        public System.Collections.ObjectModel.Collection<NullableNamespace.MyNullable<NumberNamespace.Number>[]> C_N_M_N;
    }
}";

        private IReadOnlyCollection<(string Name, ITypeSymbol TypeSymbol)> _namespaceFields = Array.Empty<(string, ITypeSymbol)>();

        [OneTimeSetUp]
        public void BeforeAnyTests()
        {
            var (_, tree, semanticModel) = Utils.CreateTestCompilation(NAMESPACE_EXTRACT_CODE, new[] { typeof(BigInteger).Assembly });

            var @class = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First(cds => cds.Identifier.ValueText == "Test");
            _namespaceFields = @class.ChildNodes().OfType<FieldDeclarationSyntax>()
                .Select(f => f.Declaration)
                .SelectMany(decl => decl.Variables.Select(v =>
                    (v.Identifier.Text, semanticModel.GetTypeInfo(decl.Type).Type ?? throw new NotSupportedException("No type info")))
                )
                .ToList();
        }

        [TestCase("Bi", "System.Numerics")]
        [TestCase("N1", "NumberNamespace")]
        [TestCase("A1", "NumberNamespace;System")]
        [TestCase("Nc1", "CollectionNamespace")]
        [TestCase("C_N_N", "NumberNamespace;System;System.Collections.ObjectModel")]
        [TestCase("C_N_M_N", "NumberNamespace;System;System.Collections.ObjectModel;NullableNamespace")]
        public void ExtractNamespaces_ShouldExtractNestedNamespaces(string symbolName, string expectedNamespacesText)
        {
            var expectedNamespaces = new SortedSet<string>(expectedNamespacesText.Split(';'));
            var symbolMeta = _namespaceFields.SingleOrDefault(p => p.Name == symbolName);
            Assert.That(symbolName, Is.Not.Null, "Initialization error");
            var namespaces = new SortedSet<string>();


            CompilerUtils.ExtractNamespaces(symbolMeta.TypeSymbol, namespaces);


            Assert.That(namespaces, Is.EquivalentTo(expectedNamespaces));
        }
    }
}
