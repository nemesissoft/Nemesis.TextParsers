using FluentAssertions;
using JetBrains.Annotations;
using Nemesis.Essentials.Design;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers.Tests.Utils;

internal static partial class TestHelper
{
    public static string AssertException(Exception actual, Type expectedException, string expectedErrorMessagePart, bool logMessage = false)
    {
        if (actual is TargetInvocationException tie && tie.InnerException is { } inner)
            actual = inner;

        Assert.That(actual, Is.TypeOf(expectedException), () => $@"Unexpected external exception: {actual}");

        if (expectedErrorMessagePart != null)
            Assert.That(
                IgnoreNewLinesComparer.RemoveNewLines(actual?.Message),
                Does.Contain(IgnoreNewLinesComparer.RemoveNewLines(expectedErrorMessagePart))
            );

        if (!logMessage) return "";

        string message = actual switch
        {
            OverflowException oe => $"Expected overflow: {oe.Message}",
            FormatException fe => $"Expected bad format: {fe.Message}",
            ArgumentException ae => $"Expected argument exception: {ae.Message}",
            InvalidOperationException ioe => $"Expected invalid operation: {ioe.Message}",
            _ => $"Expected other: {actual?.Message}"
        };

        return message;
    }

    public static void IsMutuallyEquivalent(object o1, object o2, string because = "")
    {
        if (o1 is Regex re1 && o2 is Regex re2)
        {
            Assert.Multiple(() =>
            {
                Assert.That(re1.Options, Is.EqualTo(re2.Options));
                Assert.That(re1.ToString(), Is.EqualTo(re2.ToString()));
            });
        }
        else if (o1 != null && TypeMeta.TryGetGenericRealization(o1.GetType(), typeof(ArraySegment<>), out var as1) &&
                 as1.GenericTypeArguments[0] is var ase1 &&
                 o2 != null && TypeMeta.TryGetGenericRealization(o2.GetType(), typeof(ArraySegment<>), out var as2) &&
                 as2.GenericTypeArguments[0] is var ase2 &&
                 ase1 == ase2
        )
        {
            var method = typeof(TestHelper).GetMethod(nameof(ArraySegmentEquals), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(nameof(TestHelper), nameof(ArraySegmentEquals));
            method = method.MakeGenericMethod(ase1);
            var equals = (bool)method.Invoke(null, [o1, o2]);

            Assert.That(equals, Is.True, "o1 != o2 when treated as ArraySegment");
        }
        else
        {
            o1.Should().BeEquivalentTo(o2, options => options.WithStrictOrdering(), because);
            o2.Should().BeEquivalentTo(o1, options => options.WithStrictOrdering(), because);
        }
    }

    private static bool ArraySegmentEquals<T>(ArraySegment<T> o1, ArraySegment<T> o2) =>
        EnumerableEqualityComparer<T>.DefaultInstance.Equals(o1.Array, o2.Array) &&
        o1.Offset == o2.Offset &&
        o1.Count == o2.Count;


    public static TDelegate MakeDelegate<TDelegate>(Expression<TDelegate> expression, params Type[] typeArguments)
        where TDelegate : Delegate
    {
        var method = Method.OfExpression(expression);
        if (method.IsGenericMethod)
        {
            method = method.GetGenericMethodDefinition();
            method = method.MakeGenericMethod(typeArguments);
        }

        var parameters = method.GetParameters()
            .Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToList();

        var @this = Expression.Parameter(
            method.ReflectedType ?? throw new NotSupportedException("Method type cannot be empty"), "this");

        var call = method.IsStatic
            ? Expression.Call(method, parameters)
            : Expression.Call(@this, method, parameters);

        if (!method.IsStatic)
            parameters.Insert(0, @this);

        return Expression.Lambda<TDelegate>(call, parameters).Compile();
    }

    public static void ParseAndFormat<T>(T instance, string text, ITransformerStore store = null)
    {
        var sut = (store ?? Sut.DefaultStore).GetTransformer<T>();

        var actualParsed1 = sut.Parse(text);

        string formattedInstance = sut.Format(instance);
        string formattedActualParsed = sut.Format(actualParsed1);
        Assert.That(formattedInstance, Is.EqualTo(formattedActualParsed));

        var actualParsed2 = sut.Parse(formattedInstance);

        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }

    public static void ParseAndFormatObject([NotNull] object instance, string text, ITransformerStore store = null)
    {
        ArgumentNullException.ThrowIfNull(instance);

        store ??= Sut.DefaultStore;

        Type transType = instance.GetType();

        var sut = store.GetTransformer(transType);

        string formattedInstance = sut.FormatObject(instance);

        var actualParsed1 = sut.ParseObject(text);

        string formattedActualParsed = sut.FormatObject(actualParsed1);
        Assert.That(formattedInstance, Is.EqualTo(formattedActualParsed));

        var actualParsed2 = sut.ParseObject(formattedInstance);

        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }


    public static void RoundTrip([NotNull] object instance, ITransformerStore store = null) =>
        RoundTrip(instance, (store ?? Sut.DefaultStore).GetTransformer(instance.GetType()));


    public static void RoundTrip([NotNull] object instance, ITransformer sut = null)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var text = sut.FormatObject(instance);

        var parsed1 = sut.ParseObject(text);
        var parsed2 = sut.ParseObject(text);
        IsMutuallyEquivalent(parsed1, instance);
        IsMutuallyEquivalent(parsed2, parsed1);


        var text3 = sut.FormatObject(parsed1);
        var parsed3 = sut.ParseObject(text3);

        IsMutuallyEquivalent(parsed3, instance);
        IsMutuallyEquivalent(parsed1, parsed3);
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"\W", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GetInvalidTestNamePattern();

    public static string SanitizeTestName(this string text) =>
        GetInvalidTestNamePattern().Replace(text, "_");
#else
    private static readonly Regex _invalidTestName = new(@"\W", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string SanitizeTestName(this string text) => _invalidTestName.Replace(text, "_");
#endif        
}

#nullable enable
internal class IgnoreNewLinesComparer : IComparer<string>, IEqualityComparer<string>
{
    public static readonly IComparer<string> Comparer = new IgnoreNewLinesComparer();

    public static readonly IEqualityComparer<string> EqualityComparer = new IgnoreNewLinesComparer();

    public int Compare(string? x, string? y) => string.CompareOrdinal(RemoveNewLines(x), RemoveNewLines(y));

    public bool Equals(string? x, string? y) => RemoveNewLines(x) == RemoveNewLines(y);

    public int GetHashCode(string s) => RemoveNewLines(s)?.GetHashCode() ?? 0;

    //for NET 6+ use string.ReplaceLineEndings()
    public static string? RemoveNewLines(string? s) => s?
        .Replace(Environment.NewLine, "")
        .Replace("\n", "")
        .Replace("\r", "");
}