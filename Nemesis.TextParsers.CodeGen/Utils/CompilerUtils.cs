using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Utils
{
    class CompilerUtils
    {
        public static void ExtractNamespaces(ITypeSymbol typeSymbol, ISet<string> namespaces)
        {
            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType) //namedType.TypeParameters for unbound generics
            {
                namespaces.Add(namedType.ContainingNamespace.ToDisplayString());

                foreach (var arg in namedType.TypeArguments)
                    ExtractNamespaces(arg, namespaces);
            }
            else if (typeSymbol is IArrayTypeSymbol arraySymbol)
            {
                namespaces.Add("System");

                ITypeSymbol elementSymbol = arraySymbol.ElementType;
                while (elementSymbol is IArrayTypeSymbol innerArray)
                    elementSymbol = innerArray.ElementType;

                ExtractNamespaces(elementSymbol, namespaces);
            }
            /*else if (typeSymbol.TypeKind == TypeKind.Error || typeSymbol.TypeKind == TypeKind.Dynamic)
            {
                //add appropriate reference to your compilation 
            }*/
            else
                namespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
        }
    }
}
