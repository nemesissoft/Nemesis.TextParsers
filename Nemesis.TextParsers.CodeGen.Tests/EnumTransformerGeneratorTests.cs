using FluentAssertions;
using Microsoft.CodeAnalysis;
using Nemesis.TextParsers.CodeGen.Enums;
using static Nemesis.TextParsers.CodeGen.Tests.CodeGenUtils;

namespace Nemesis.TextParsers.CodeGen.Tests;

[TestFixture]
internal class EnumTransformerGeneratorTests
{
    private static IEnumerable<TCD> EnumCodeGenTestCases() =>
        EnumCodeGenCases.Select((t, i) =>
            new TCD(t.source, t.expectedMeta, t.expectedCodeGen)
            .SetName($"EnumCodeGen_{i + 1:00}_{t.name}"));

    [TestCaseSource(nameof(EnumCodeGenTestCases))]
    public void Generate_ShouldReturnValid_MetaInput_And_Output(string source, TransformerMeta expectedMeta, string expectedCodeGen)
    {
        var compilation = CreateValidCompilation(source);

        var (sources, metas) = new EnumTransformerGenerator().RunIncrementalGeneratorAndCaptureInputs<TransformerMeta>(compilation);

        Assert.Multiple(() =>
        {
            Assert.That(metas, Has.Count.EqualTo(1));
            Assert.That(sources, Has.Count.EqualTo(1));

            var meta = metas[0];
            meta.Should().BeEquivalentTo(expectedMeta);

            var source = ScrubGeneratorComments(sources.First());
            Assert.That(source, Is.EqualTo(expectedCodeGen).Using(IgnoreNewLinesComparer.EqualityComparer));
        });
    }

    [Test]
    public void Diagnostics_ShouldReport_BadEnums_WhenCaseCouldCarryWeight()
    {
        var compilation = CreateValidCompilation("""
          [Auto.AutoEnumTransformer(CaseInsensitive = true)]
          internal enum Casing { A, a, B, b, C, c, Good }
          """);

        var result = new EnumTransformerGenerator().RunIncrementalGenerator(compilation);


        var diagnostics = result.Diagnostics;
        Assert.That(diagnostics, Has.Length.EqualTo(1));

        var diagnostic = diagnostics.Single();
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Descriptor.Id, Is.EqualTo(EnumTransformerGenerator.CaseInsensitiveIncompatibleMemberNames.Id));
            var diag = diagnostic.ToString();
            Assert.That(diag, Does.StartWith("(2,15)"));
            Assert.That(diag, Does.Contain("<global namespace>.Casing"));
        });
    }

    internal static readonly IEnumerable<(string name, string source, TransformerMeta expectedMeta, string expectedCodeGen)> EnumCodeGenCases =
    [
        ("Month", """
          [Auto.AutoEnumTransformer(CaseInsensitive = true, AllowParsingNumerics = true, TransformerClassName = "MonthCodeGenTransformer")]
          public enum Month : byte
          {
              None = 0, 
              January = 1, February = 2, March = 3, April = 4, May = 5, June = 6,
              July = 7, August = 8, September = 9, October = 10, November = 11, December = 12
          }
          """,
          new("MonthCodeGenTransformer", "", true, true, "Month", ["None", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"], true, false, "byte"),
        """
         //HEAD

         using System;
         using Nemesis.TextParsers;

         [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
         [System.Runtime.CompilerServices.CompilerGenerated]
         public sealed class MonthCodeGenTransformer : TransformerBase<Month>
         {
             public override string Format(Month element) => element switch
             {
                 Month.None => nameof(Month.None),
                 Month.January => nameof(Month.January),
                 Month.February => nameof(Month.February),
                 Month.March => nameof(Month.March),
                 Month.April => nameof(Month.April),
                 Month.May => nameof(Month.May),
                 Month.June => nameof(Month.June),
                 Month.July => nameof(Month.July),
                 Month.August => nameof(Month.August),
                 Month.September => nameof(Month.September),
                 Month.October => nameof(Month.October),
                 Month.November => nameof(Month.November),
                 Month.December => nameof(Month.December),
                 _ => element.ToString("G"),
             };

             protected override Month ParseCore(in ReadOnlySpan<char> input) =>
                 input.IsWhiteSpace() ? default : (Month)ParseElement(input);

             private static byte ParseElement(ReadOnlySpan<char> input)
             {
                 if (input.IsEmpty || input.IsWhiteSpace()) return default;
                 input = input.Trim();
                 if (IsNumeric(input) && byte.TryParse(input
         #if NETFRAMEWORK
             .ToString() //legacy frameworks do not support parsing from ReadOnlySpan<char>
         #endif
                     , out var number))
                     return number;
                 else
                     return ParseName(input);


                 static bool IsNumeric(ReadOnlySpan<char> input) =>
                     input.Length > 0 && input[0] is var first &&
                     (char.IsDigit(first) || first is '-' or '+');    
             }

             private static byte ParseName(ReadOnlySpan<char> input)
             {    
                 if (IsEqual(input, nameof(Month.None)))
                     return (byte)Month.None;            

                 else if (IsEqual(input, nameof(Month.January)))
                     return (byte)Month.January;            

                 else if (IsEqual(input, nameof(Month.February)))
                     return (byte)Month.February;            

                 else if (IsEqual(input, nameof(Month.March)))
                     return (byte)Month.March;            

                 else if (IsEqual(input, nameof(Month.April)))
                     return (byte)Month.April;            

                 else if (IsEqual(input, nameof(Month.May)))
                     return (byte)Month.May;            

                 else if (IsEqual(input, nameof(Month.June)))
                     return (byte)Month.June;            

                 else if (IsEqual(input, nameof(Month.July)))
                     return (byte)Month.July;            

                 else if (IsEqual(input, nameof(Month.August)))
                     return (byte)Month.August;            

                 else if (IsEqual(input, nameof(Month.September)))
                     return (byte)Month.September;            

                 else if (IsEqual(input, nameof(Month.October)))
                     return (byte)Month.October;            

                 else if (IsEqual(input, nameof(Month.November)))
                     return (byte)Month.November;            

                 else if (IsEqual(input, nameof(Month.December)))
                     return (byte)Month.December;            

                 else throw new FormatException(@$"Enum of type 'Month' cannot be parsed from '{input.ToString()}'.
         Valid values are: [None or January or February or March or April or May or June or July or August or September or October or November or December] or number within byte range. 
         Ignore case option on.");        

                 static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                     MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
             }
         }
         """),


        ("Months", """
          [Auto.AutoEnumTransformer(CaseInsensitive = false, AllowParsingNumerics = false, TransformerClassNamespace = "SpecialNamespace")]
          [System.Flags]
          internal enum Months : short
          {
              None = 0,
              January
                  = 0b0000_0000_0001,
              February
                  = 0b0000_0000_0010,
              March
                  = 0b0000_0000_0100,
              April
                  = 0b0000_0000_1000,
              May
                  = 0b0000_0001_0000,
              June
                  = 0b0000_0010_0000,
              July
                  = 0b0000_0100_0000,
              August
                  = 0b0000_1000_0000,
              September
                  = 0b0001_0000_0000,
              October
                  = 0b0010_0000_0000,
              November
                  = 0b0100_0000_0000,
              December
                  = 0b1000_0000_0000,

              Summer = July | August | September,
              All = January | February | March | April |
                    May | June | July | August |
                    September | October | November | December
          }
          """,
          new("MonthsTransformer", "SpecialNamespace", false, false, "Months", ["None", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "Summer", "All"], false, true, "short"),
        """
         //HEAD

         using System;
         using Nemesis.TextParsers;
         namespace SpecialNamespace;

         [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
         [System.Runtime.CompilerServices.CompilerGenerated]
         internal sealed class MonthsTransformer : TransformerBase<Months>
         {
             public override string Format(Months element) => element switch
             {
                 Months.None => nameof(Months.None),
                 Months.January => nameof(Months.January),
                 Months.February => nameof(Months.February),
                 Months.March => nameof(Months.March),
                 Months.April => nameof(Months.April),
                 Months.May => nameof(Months.May),
                 Months.June => nameof(Months.June),
                 Months.July => nameof(Months.July),
                 Months.August => nameof(Months.August),
                 Months.September => nameof(Months.September),
                 Months.October => nameof(Months.October),
                 Months.November => nameof(Months.November),
                 Months.December => nameof(Months.December),
                 Months.Summer => nameof(Months.Summer),
                 Months.All => nameof(Months.All),
                 _ => element.ToString("G"),
             };

             protected override Months ParseCore(in ReadOnlySpan<char> input)
             {
                 if (input.IsWhiteSpace()) return default;

                 var enumStream = input.Split(',').GetEnumerator();

                 if (!enumStream.MoveNext()) 
                     throw new FormatException($"At least one element is expected to parse 'Months' enum");
                 var currentValue = ParseElement(enumStream.Current);

                 while (enumStream.MoveNext())
                 {
                     var element = ParseElement(enumStream.Current);

                     currentValue = (short)(currentValue | element);
                 }

                 return (Months)currentValue;
             }

             private static short ParseElement(ReadOnlySpan<char> input)
             {
                 if (input.IsEmpty || input.IsWhiteSpace()) return default;
                 input = input.Trim();
                 return ParseName(input);
             }

             private static short ParseName(ReadOnlySpan<char> input)
             {    
                 if (IsEqual(input, nameof(Months.None)))
                     return (short)Months.None;            

                 else if (IsEqual(input, nameof(Months.January)))
                     return (short)Months.January;            

                 else if (IsEqual(input, nameof(Months.February)))
                     return (short)Months.February;            

                 else if (IsEqual(input, nameof(Months.March)))
                     return (short)Months.March;            

                 else if (IsEqual(input, nameof(Months.April)))
                     return (short)Months.April;            

                 else if (IsEqual(input, nameof(Months.May)))
                     return (short)Months.May;            

                 else if (IsEqual(input, nameof(Months.June)))
                     return (short)Months.June;            

                 else if (IsEqual(input, nameof(Months.July)))
                     return (short)Months.July;            

                 else if (IsEqual(input, nameof(Months.August)))
                     return (short)Months.August;            

                 else if (IsEqual(input, nameof(Months.September)))
                     return (short)Months.September;            

                 else if (IsEqual(input, nameof(Months.October)))
                     return (short)Months.October;            

                 else if (IsEqual(input, nameof(Months.November)))
                     return (short)Months.November;            

                 else if (IsEqual(input, nameof(Months.December)))
                     return (short)Months.December;            

                 else if (IsEqual(input, nameof(Months.Summer)))
                     return (short)Months.Summer;            

                 else if (IsEqual(input, nameof(Months.All)))
                     return (short)Months.All;            

                 else throw new FormatException(@$"Enum of type 'Months' cannot be parsed from '{input.ToString()}'.
         Valid values are: [None or January or February or March or April or May or June or July or August or September or October or November or December or Summer or All]. 
         Case sensitive option on.");        

                 static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                     MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.Ordinal);
             }
         }
         """),

        ("DaysOfWeek", """
          [Auto.AutoEnumTransformer(CaseInsensitive = true, AllowParsingNumerics = true)]
          [System.Flags]
          enum DaysOfWeek : byte
          {
              None = 0,
              Monday
                  = 0b0000_0001,
              Tuesday
                  = 0b0000_0010,
              Wednesday
                  = 0b0000_0100,
              Thursday
                  = 0b0000_1000,
              Friday
                  = 0b0001_0000,
              Saturday
                  = 0b0010_0000,
              Sunday
                  = 0b0100_0000,

              Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
              Weekends = Saturday | Sunday,
              All = Weekdays | Weekends
          }
          """,
          new("DaysOfWeekTransformer", "", true, true, "DaysOfWeek", ["None", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday", "Weekdays", "Weekends", "All"], false, true, "byte"),
        """
         //HEAD

         using System;
         using Nemesis.TextParsers;

         [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
         [System.Runtime.CompilerServices.CompilerGenerated]
         internal sealed class DaysOfWeekTransformer : TransformerBase<DaysOfWeek>
         {
             public override string Format(DaysOfWeek element) => element switch
             {
                 DaysOfWeek.None => nameof(DaysOfWeek.None),
                 DaysOfWeek.Monday => nameof(DaysOfWeek.Monday),
                 DaysOfWeek.Tuesday => nameof(DaysOfWeek.Tuesday),
                 DaysOfWeek.Wednesday => nameof(DaysOfWeek.Wednesday),
                 DaysOfWeek.Thursday => nameof(DaysOfWeek.Thursday),
                 DaysOfWeek.Friday => nameof(DaysOfWeek.Friday),
                 DaysOfWeek.Saturday => nameof(DaysOfWeek.Saturday),
                 DaysOfWeek.Sunday => nameof(DaysOfWeek.Sunday),
                 DaysOfWeek.Weekdays => nameof(DaysOfWeek.Weekdays),
                 DaysOfWeek.Weekends => nameof(DaysOfWeek.Weekends),
                 DaysOfWeek.All => nameof(DaysOfWeek.All),
                 _ => element.ToString("G"),
             };

             protected override DaysOfWeek ParseCore(in ReadOnlySpan<char> input)
             {
                 if (input.IsWhiteSpace()) return default;

                 var enumStream = input.Split(',').GetEnumerator();

                 if (!enumStream.MoveNext()) 
                     throw new FormatException($"At least one element is expected to parse 'DaysOfWeek' enum");
                 var currentValue = ParseElement(enumStream.Current);

                 while (enumStream.MoveNext())
                 {
                     var element = ParseElement(enumStream.Current);

                     currentValue = (byte)(currentValue | element);
                 }

                 return (DaysOfWeek)currentValue;
             }

             private static byte ParseElement(ReadOnlySpan<char> input)
             {
                 if (input.IsEmpty || input.IsWhiteSpace()) return default;
                 input = input.Trim();
                 if (IsNumeric(input) && byte.TryParse(input
         #if NETFRAMEWORK
             .ToString() //legacy frameworks do not support parsing from ReadOnlySpan<char>
         #endif
                     , out var number))
                     return number;
                 else
                     return ParseName(input);


                 static bool IsNumeric(ReadOnlySpan<char> input) =>
                     input.Length > 0 && input[0] is var first &&
                     (char.IsDigit(first) || first is '-' or '+');    
             }

             private static byte ParseName(ReadOnlySpan<char> input)
             {    
                 if (IsEqual(input, nameof(DaysOfWeek.None)))
                     return (byte)DaysOfWeek.None;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Monday)))
                     return (byte)DaysOfWeek.Monday;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Tuesday)))
                     return (byte)DaysOfWeek.Tuesday;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Wednesday)))
                     return (byte)DaysOfWeek.Wednesday;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Thursday)))
                     return (byte)DaysOfWeek.Thursday;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Friday)))
                     return (byte)DaysOfWeek.Friday;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Saturday)))
                     return (byte)DaysOfWeek.Saturday;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Sunday)))
                     return (byte)DaysOfWeek.Sunday;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Weekdays)))
                     return (byte)DaysOfWeek.Weekdays;            

                 else if (IsEqual(input, nameof(DaysOfWeek.Weekends)))
                     return (byte)DaysOfWeek.Weekends;            

                 else if (IsEqual(input, nameof(DaysOfWeek.All)))
                     return (byte)DaysOfWeek.All;            

                 else throw new FormatException(@$"Enum of type 'DaysOfWeek' cannot be parsed from '{input.ToString()}'.
         Valid values are: [None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All] or number within byte range. 
         Ignore case option on.");        

                 static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                     MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
             }
         }
         """),





        ("EmptyEnum", """
          [Auto.AutoEnumTransformer(CaseInsensitive = false)]
          internal enum EmptyEnum: ulong { }
          """,
          new("EmptyEnumTransformer", "", false, true, "EmptyEnum", [], false, false, "ulong"),
        """
         //HEAD

         using System;
         using Nemesis.TextParsers;

         [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
         [System.Runtime.CompilerServices.CompilerGenerated]
         internal sealed class EmptyEnumTransformer : TransformerBase<EmptyEnum>
         {
             public override string Format(EmptyEnum element) => element switch
             {
                 _ => element.ToString("G"),
             };

             protected override EmptyEnum ParseCore(in ReadOnlySpan<char> input) =>
                 input.IsWhiteSpace() ? default : (EmptyEnum)ParseElement(input);

             private static ulong ParseElement(ReadOnlySpan<char> input)
             {
                 if (input.IsEmpty || input.IsWhiteSpace()) return default;
                 input = input.Trim();
                 if (IsNumeric(input) && ulong.TryParse(input
         #if NETFRAMEWORK
             .ToString() //legacy frameworks do not support parsing from ReadOnlySpan<char>
         #endif
                     , out var number))
                     return number;
                 else
                     return ParseName(input);


                 static bool IsNumeric(ReadOnlySpan<char> input) =>
                     input.Length > 0 && input[0] is var first &&
                     (char.IsDigit(first) || first is '-' or '+');    
             }

             private static ulong ParseName(ReadOnlySpan<char> input)
             {    
                 throw new FormatException(@$"Enum of type 'EmptyEnum' cannot be parsed from '{input.ToString()}'.
         Valid values are: [] or number within ulong range. 
         Case sensitive option on.");        
             }
         }
         """),


        ("SByteEnum", """
          [Auto.AutoEnumTransformer(AllowParsingNumerics = false)]
          enum SByteEnum : sbyte { Sb1 = -10, Sb2 = 0, Sb3 = 5, Sb4 = 10 }
          """,
          new("SByteEnumTransformer", "", true, false, "SByteEnum", ["Sb1", "Sb2", "Sb3", "Sb4"], false, false, "sbyte"),
        """
         //HEAD

         using System;
         using Nemesis.TextParsers;

         [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
         [System.Runtime.CompilerServices.CompilerGenerated]
         internal sealed class SByteEnumTransformer : TransformerBase<SByteEnum>
         {
             public override string Format(SByteEnum element) => element switch
             {
                 SByteEnum.Sb1 => nameof(SByteEnum.Sb1),
                 SByteEnum.Sb2 => nameof(SByteEnum.Sb2),
                 SByteEnum.Sb3 => nameof(SByteEnum.Sb3),
                 SByteEnum.Sb4 => nameof(SByteEnum.Sb4),
                 _ => element.ToString("G"),
             };

             protected override SByteEnum ParseCore(in ReadOnlySpan<char> input) =>
                 input.IsWhiteSpace() ? default : (SByteEnum)ParseElement(input);

             private static sbyte ParseElement(ReadOnlySpan<char> input)
             {
                 if (input.IsEmpty || input.IsWhiteSpace()) return default;
                 input = input.Trim();
                 return ParseName(input);
             }

             private static sbyte ParseName(ReadOnlySpan<char> input)
             {    
                 if (IsEqual(input, nameof(SByteEnum.Sb1)))
                     return (sbyte)SByteEnum.Sb1;            

                 else if (IsEqual(input, nameof(SByteEnum.Sb2)))
                     return (sbyte)SByteEnum.Sb2;            

                 else if (IsEqual(input, nameof(SByteEnum.Sb3)))
                     return (sbyte)SByteEnum.Sb3;            

                 else if (IsEqual(input, nameof(SByteEnum.Sb4)))
                     return (sbyte)SByteEnum.Sb4;            

                 else throw new FormatException(@$"Enum of type 'SByteEnum' cannot be parsed from '{input.ToString()}'.
         Valid values are: [Sb1 or Sb2 or Sb3 or Sb4]. 
         Ignore case option on.");        

                 static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                     MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
             }
         }
         """),


        ("Int64Enum", """
          namespace ContainingNamespace;

          [Auto.AutoEnumTransformer(CaseInsensitive = false, AllowParsingNumerics = false)]
          enum Int64Enum : long { L1 = -50, L2 = 0, L3 = 1, L4 = 50 }
          """,
          new("Int64EnumTransformer", "ContainingNamespace", false, false, "ContainingNamespace.Int64Enum", ["L1", "L2", "L3", "L4"], false, false, "long"),
        """
         //HEAD

         using System;
         using Nemesis.TextParsers;
         namespace ContainingNamespace;

         [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
         [System.Runtime.CompilerServices.CompilerGenerated]
         internal sealed class Int64EnumTransformer : TransformerBase<ContainingNamespace.Int64Enum>
         {
             public override string Format(ContainingNamespace.Int64Enum element) => element switch
             {
                 ContainingNamespace.Int64Enum.L1 => nameof(ContainingNamespace.Int64Enum.L1),
                 ContainingNamespace.Int64Enum.L2 => nameof(ContainingNamespace.Int64Enum.L2),
                 ContainingNamespace.Int64Enum.L3 => nameof(ContainingNamespace.Int64Enum.L3),
                 ContainingNamespace.Int64Enum.L4 => nameof(ContainingNamespace.Int64Enum.L4),
                 _ => element.ToString("G"),
             };

             protected override ContainingNamespace.Int64Enum ParseCore(in ReadOnlySpan<char> input) =>
                 input.IsWhiteSpace() ? default : (ContainingNamespace.Int64Enum)ParseElement(input);

             private static long ParseElement(ReadOnlySpan<char> input)
             {
                 if (input.IsEmpty || input.IsWhiteSpace()) return default;
                 input = input.Trim();
                 return ParseName(input);
             }

             private static long ParseName(ReadOnlySpan<char> input)
             {    
                 if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L1)))
                     return (long)ContainingNamespace.Int64Enum.L1;            

                 else if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L2)))
                     return (long)ContainingNamespace.Int64Enum.L2;            

                 else if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L3)))
                     return (long)ContainingNamespace.Int64Enum.L3;            

                 else if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L4)))
                     return (long)ContainingNamespace.Int64Enum.L4;            

                 else throw new FormatException(@$"Enum of type 'ContainingNamespace.Int64Enum' cannot be parsed from '{input.ToString()}'.
         Valid values are: [L1 or L2 or L3 or L4]. 
         Case sensitive option on.");        

                 static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                     MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.Ordinal);
             }
         }
         """),



        ("Casing", """
          [Auto.AutoEnumTransformer(CaseInsensitive = false)]
          enum Casing { A, a, B, b, C, c, Good }
          """,
          new("CasingTransformer", "", false, true, "Casing", ["A", "a", "B", "b", "C", "c", "Good"], false, false, "int"),
        """
         //HEAD

         using System;
         using Nemesis.TextParsers;

         [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
         [System.Runtime.CompilerServices.CompilerGenerated]
         internal sealed class CasingTransformer : TransformerBase<Casing>
         {
             public override string Format(Casing element) => element switch
             {
                 Casing.A => nameof(Casing.A),
                 Casing.a => nameof(Casing.a),
                 Casing.B => nameof(Casing.B),
                 Casing.b => nameof(Casing.b),
                 Casing.C => nameof(Casing.C),
                 Casing.c => nameof(Casing.c),
                 Casing.Good => nameof(Casing.Good),
                 _ => element.ToString("G"),
             };

             protected override Casing ParseCore(in ReadOnlySpan<char> input) =>
                 input.IsWhiteSpace() ? default : (Casing)ParseElement(input);

             private static int ParseElement(ReadOnlySpan<char> input)
             {
                 if (input.IsEmpty || input.IsWhiteSpace()) return default;
                 input = input.Trim();
                 if (IsNumeric(input) && int.TryParse(input
         #if NETFRAMEWORK
             .ToString() //legacy frameworks do not support parsing from ReadOnlySpan<char>
         #endif
                     , out var number))
                     return number;
                 else
                     return ParseName(input);


                 static bool IsNumeric(ReadOnlySpan<char> input) =>
                     input.Length > 0 && input[0] is var first &&
                     (char.IsDigit(first) || first is '-' or '+');    
             }

             private static int ParseName(ReadOnlySpan<char> input)
             {    
                 if (IsEqual(input, nameof(Casing.A)))
                     return (int)Casing.A;            

                 else if (IsEqual(input, nameof(Casing.a)))
                     return (int)Casing.a;            

                 else if (IsEqual(input, nameof(Casing.B)))
                     return (int)Casing.B;            

                 else if (IsEqual(input, nameof(Casing.b)))
                     return (int)Casing.b;            

                 else if (IsEqual(input, nameof(Casing.C)))
                     return (int)Casing.C;            

                 else if (IsEqual(input, nameof(Casing.c)))
                     return (int)Casing.c;            

                 else if (IsEqual(input, nameof(Casing.Good)))
                     return (int)Casing.Good;            

                 else throw new FormatException(@$"Enum of type 'Casing' cannot be parsed from '{input.ToString()}'.
         Valid values are: [A or a or B or b or C or c or Good] or number within int range. 
         Case sensitive option on.");        

                 static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                     MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.Ordinal);
             }
         }
         """)
    ];
}
