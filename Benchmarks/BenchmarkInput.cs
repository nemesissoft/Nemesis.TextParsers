using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Benchmarks
{
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
                   _ => value.ToString()
               };
    }
}
