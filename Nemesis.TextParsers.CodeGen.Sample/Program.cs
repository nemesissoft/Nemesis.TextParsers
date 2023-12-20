#nullable enable
using System.Diagnostics;
using Nemesis.TextParsers;
using Nemesis.TextParsers.CodeGen.Sample;

FormatAndParse(Month.April, "April");
FormatAndParse(Months.April | Months.May, "April, May");
FormatAndParse(Months.July | Months.August | Months.September, "Summer");

//new StructPoint3d(1.23, 4.56, 7.89).DebuggerHook();
FormatAndParse(new StructPoint3d(1.23, 4.56, 7.89), "〈1.23_4.56_7.89〉");

FormatAndParse(new RecordPoint3d(1.23, 4.56, 7.89), "⟪1.23,4.56,7.89⟫");
FormatAndParse(new RecordPoint2d(1.23, 4.56), "(1.23;4.56)");


static void FormatAndParse<T>(T instance, string text)
{
    var sut = TextTransformer.Default.GetTransformer<T>();

    var actualFormatted = sut.Format(instance);
    Console.WriteLine(actualFormatted);
    Assert(actualFormatted == text);


    var actualParsed1 = sut.Parse(text);
    var actualParsed2 = sut.Parse(actualFormatted);
    Assert(actualParsed1?.Equals(instance));
    Assert(actualParsed2?.Equals(instance));
    Assert(actualParsed1?.Equals(actualParsed2));
}

static void Assert(bool? condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
    => Debug.Assert(condition.HasValue && condition.Value, message);
