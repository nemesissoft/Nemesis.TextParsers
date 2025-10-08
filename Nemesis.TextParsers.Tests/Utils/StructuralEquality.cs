using System.Collections;
using Nemesis.Essentials.Design;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Tests.Utils;

public static class StructuralEquality
{
    public static bool Equals<TValue>(TValue left, TValue right)
    {
        if (left == null) return right == null;
        if (right == null) return false;

        if (ReferenceEquals(left, right)) return true;

        var type = typeof(TValue);
        if (left is IEquatable<TValue> eq1)
            return eq1.Equals(right);
        else if (type.DerivesOrImplementsGeneric(typeof(IEnumerable<>)))
        {
            var enumerableTypeArguments = type.IsArray ? type.GetElementType() : GetIEnumerableTypeParameter(type);

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(enumerableTypeArguments);
            var comparerType = typeof(EnumerableEqualityComparer<>).MakeGenericType(enumerableTypeArguments);

            FieldInfo comparerInstanceField = comparerType.GetField(nameof(EnumerableEqualityComparer<>.DefaultInstance));
            var comparer = comparerInstanceField.GetValue(null);

            MethodInfo equalsMethod = comparerInstanceField.FieldType.GetMethod(nameof(IEqualityComparer.Equals), [enumerableType, enumerableType])
                                      ?? throw new MissingFieldException("EnumerableEqualityComparer<>.DefaultInstance field is missing");

            var result = (bool)equalsMethod.Invoke(comparer, [left, right]);

            return result;
        }
        else
            return object.Equals(left, right);
    }

    private static Type GetIEnumerableTypeParameter([JetBrains.Annotations.NotNull] Type type)
    {        
        if (type == null) throw new ArgumentNullException(nameof(type));
        var generic = typeof(IEnumerable<>);

        Type retType = null;

        foreach (Type @interface in type.GetInterfaces())
            if (@interface.IsGenericType && !@interface.IsGenericTypeDefinition && @interface.GetGenericTypeDefinition() == generic)
                if (retType == null)
                    retType = @interface;
                else
                    throw new NotSupportedException($"This path only supports classes that implement {typeof(IEnumerable<object>).Name} interface only once");

        return retType != null && retType.GenericTypeArguments.Length == 1
            ? retType.GenericTypeArguments[0]
            : throw new InvalidOperationException("Type has to be or implement IEnumerable<>");

    }
}

public class StructuralEqualityComparer<T> : IEqualityComparer<T>
{
    public static readonly IEqualityComparer<T> Instance = new StructuralEqualityComparer<T>();

    private StructuralEqualityComparer() { }

    public bool Equals(T x, T y) => StructuralEquality.Equals(x, y);

    public int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;
}
