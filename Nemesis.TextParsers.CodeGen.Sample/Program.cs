using System;
using System.Diagnostics;

namespace Nemesis.TextParsers.CodeGen.Sample
{
    class Program
    {
        static void Main()
        {
            new StructPoint3d(1.23, 4.56, 7.89).DebuggerHook();
            FormatAndParse(new StructPoint3d(1.23, 4.56, 7.89), "〈1.23_4.56_7.89〉");

            FormatAndParse(new RecordPoint3d(1.23, 4.56, 7.89), "⟪1.23,4.56,7.89⟫");
            FormatAndParse(new RecordPoint2d(1.23, 4.56), "(1.23;4.56)");
        }

        private static void FormatAndParse<T>(T instance, string text)
        {
            var sut = TextTransformer.Default.GetTransformer<T>();

            var actualFormatted = sut.Format(instance);
            Console.WriteLine(actualFormatted);
            Debug.Assert(actualFormatted == text, "actualFormatted == text");


            var actualParsed1 = sut.Parse(text);
            var actualParsed2 = sut.Parse(actualFormatted);
            Debug.Assert(actualParsed1.Equals(instance), "actualParsed1.Equals(instance)");
            Debug.Assert(actualParsed2.Equals(instance), "actualParsed2.Equals(instance)");
            Debug.Assert(actualParsed1.Equals(actualParsed2), "actualParsed1.Equals(actualParsed2)");

            Console.WriteLine();
        }
    }
}
