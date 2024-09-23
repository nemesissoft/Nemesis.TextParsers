using System.Collections;

namespace Benchmarks;

public class BenchmarkInput<T>
{
    public T Value { get; }
    public string Name { get; }

    public BenchmarkInput(T value, string name = null)
    {
        Value = value;
        Name = name;
    }

    public override string ToString() => !string.IsNullOrWhiteSpace(Name) ? Name : FormatValue(Value);

    private static string FormatValue(object value) =>
           value switch
           {
               null => "∅",
               bool b => b ? "true" : "false",
               string s => $"\"{s}\"",
               char c => $"\'{c}\'",
               DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
               IFormattable @if => @if.ToString(null, CultureInfo.InvariantCulture),
               IEnumerable ie => "[" + string.Join(", ", ie.Cast<object>().Select(FormatValue)) + "]",

               (var a, var b) => $"({FormatValue(a)},{FormatValue(b)})",
               (var a, var b, var c) => $"({FormatValue(a)},{FormatValue(b)},{FormatValue(c)})",
               (var a, var b, var c, var d) => $"({FormatValue(a)},{FormatValue(b)},{FormatValue(c)},{FormatValue(d)})",
               (var a, var b, var c, var d, var e) => $"({FormatValue(a)},{FormatValue(b)},{FormatValue(c)},{FormatValue(d)},{FormatValue(e)})",

               _ => value.ToString()
           };
}
