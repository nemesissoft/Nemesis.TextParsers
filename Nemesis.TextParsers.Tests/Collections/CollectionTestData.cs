﻿using System.Collections;
using System.Collections.ObjectModel;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests.Collections;

internal class CollectionTestData
{
    public static IEnumerable<(Type elementType, IEnumerable expectedOutput, string input)> ListCompoundData() => new (Type, IEnumerable, string)[]
    {
        //(typeof(int), new List<int>(), @""), (typeof(string), new List<string>(), @""),

        (typeof(byte), GetTestNumbers<byte>(byte.MinValue, byte.MaxValue-1, 1, (n1, n2) => (byte)(n1+n2)),
            @"∅|  1 | 2 |3 | 4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126|127|128|129|130|131|132|133|134|135|136|137|138|139|140|141|142|143|144|145|146|147|148|149|150|151|152|153|154|155|156|157|158|159|160|161|162|163|164|165|166|167|168|169|170|171|172|173|174|175|176|177|178|179|180|181|182|183|184|185|186|187|188|189|190|191|192|193|194|195|196|197|198|199|200|201|202|203|204|205|206|207|208|209|210|211|212|213|214|215|216|217|218|219|220|221|222|223|224|225|226|227|228|229|230|231|232|233|234|235|236|237|238|239|240|241|242|243|244|245|246|247|248|249|250|251|252|253|254" ),

        (typeof(sbyte), GetTestNumbers<sbyte>(sbyte.MinValue, sbyte.MaxValue-1, 1, (n1, n2) => (sbyte)(n1+n2)),
            @"-128|  -127 | -126 |-125 | -124|-123|-122|-121|-120|-119|-118|-117|-116|-115|-114|-113|-112|-111|-110|-109|-108|-107|-106|-105|-104|-103|-102|-101|-100|-99|-98|-97|-96|-95|-94|-93|-92|-91|-90|-89|-88|-87|-86|-85|-84|-83|-82|-81|-80|-79|-78|-77|-76|-75|-74|-73|-72|-71|-70|-69|-68|-67|-66|-65|-64|-63|-62|-61|-60|-59|-58|-57|-56|-55|-54|-53|-52|-51|-50|-49|-48|-47|-46|-45|-44|-43|-42|-41|-40|-39|-38|-37|-36|-35|-34|-33|-32|-31|-30|-29|-28|-27|-26|-25|-24|-23|-22|-21|-20|-19|-18|-17|-16|-15|-14|-13|-12|-11|-10|-9|-8|-7|-6|-5|-4|-3|-2|-1|0|1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126" ),

        (typeof(short), GetTestNumbers<short>(100),
            @"-32768|  -32441 | -32114 | -31787|-31460 |-31133|-30806|-30479|-30152|-29825|-29498|-29171|-28844|-28517|-28190|-27863|-27536|-27209|-26882|-26555|-26228|-25901|-25574|-25247|-24920|-24593|-24266|-23939|-23612|-23285|-22958|-22631|-22304|-21977|-21650|-21323|-20996|-20669|-20342|-20015|-19688|-19361|-19034|-18707|-18380|-18053|-17726|-17399|-17072|-16745|-16418|-16091|-15764|-15437|-15110|-14783|-14456|-14129|-13802|-13475|-13148|-12821|-12494|-12167|-11840|-11513|-11186|-10859|-10532|-10205|-9878|-9551|-9224|-8897|-8570|-8243|-7916|-7589|-7262|-6935|-6608|-6281|-5954|-5627|-5300|-4973|-4646|-4319|-3992|-3665|-3338|-3011|-2684|-2357|-2030|-1703|-1376|-1049|-722|-395|-68|259|586|913|1240|1567|1894|2221|2548|2875|3202|3529|3856|4183|4510|4837|5164|5491|5818|6145|6472|6799|7126|7453|7780|8107|8434|8761|9088|9415|9742|10069|10396|10723|11050|11377|11704|12031|12358|12685|13012|13339|13666|13993|14320|14647|14974|15301|15628|15955|16282|16609|16936|17263|17590|17917|18244|18571|18898|19225|19552|19879|20206|20533|20860|21187|21514|21841|22168|22495|22822|23149|23476|23803|24130|24457|24784|25111|25438|25765|26092|26419|26746|27073|27400|27727|28054|28381|28708|29035|29362|29689|30016|30343|30670|30997|31324|31651|31978|32305" ),

        (typeof(ushort), GetTestNumbers<ushort>(100),
            @"0|  655 | 1310 | 1965|2620 |3275|3930|4585|5240|5895|6550|7205|7860|8515|9170|9825|10480|11135|11790|12445|13100|13755|14410|15065|15720|16375|17030|17685|18340|18995|19650|20305|20960|21615|22270|22925|23580|24235|24890|25545|26200|26855|27510|28165|28820|29475|30130|30785|31440|32095|32750|33405|34060|34715|35370|36025|36680|37335|37990|38645|39300|39955|40610|41265|41920|42575|43230|43885|44540|45195|45850|46505|47160|47815|48470|49125|49780|50435|51090|51745|52400|53055|53710|54365|55020|55675|56330|56985|57640|58295|58950|59605|60260|60915|61570|62225|62880|63535|64190|64845" ),

        (typeof(int), GetTestNumbers<int>(10),
            @"-2147483648|  -1932735284 | -1717986920 | -1503238556|-1288490192 |-1073741828|-858993464|-644245100|-429496736|-214748372|-8|214748356|429496720|644245084|858993448|1073741812|1288490176|1503238540|1717986904|1932735268" ),

        (typeof(uint), GetTestNumbers<uint>(10),
            @"0|  429496729 | 858993458 | 1288490187|1717986916 |2147483645|2576980374|3006477103|3435973832|3865470561" ),

        (typeof(long), GetTestNumbers<long>(10),
            @"-9223372036854775808|  -8301034833169298228 | -7378697629483820648 | -6456360425798343068|-5534023222112865488 |-4611686018427387908|-3689348814741910328|-2767011611056432748|-1844674407370955168|-922337203685477588|-8|922337203685477572|1844674407370955152|2767011611056432732|3689348814741910312|4611686018427387892|5534023222112865472|6456360425798343052|7378697629483820632|8301034833169298212" ),

        (typeof(ulong), GetTestNumbers<ulong>(10),
            @"0|  1844674407370955161 | 3689348814741910322 | 5534023222112865483|7378697629483820644 |9223372036854775805|11068046444225730966|12912720851596686127|14757395258967641288|16602069666338596449" ),

#if NET7_0_OR_GREATER
        (typeof(Int128), GetTestNumbers<Int128>(10),
            @"-170141183460469231731687303715884105728|-153127065114422308558518573344295695156|-136112946768375385385349842972707284584|-119098828422328462212181112601118874012|-102084710076281539039012382229530463440|-85070591730234615865843651857942052868|-68056473384187692692674921486353642296|-51042355038140769519506191114765231724|-34028236692093846346337460743176821152|-17014118346046923173168730371588410580|-8|17014118346046923173168730371588410564|34028236692093846346337460743176821136|51042355038140769519506191114765231708|68056473384187692692674921486353642280|85070591730234615865843651857942052852|102084710076281539039012382229530463424|119098828422328462212181112601118873996|136112946768375385385349842972707284568|153127065114422308558518573344295695140" ),

        (typeof(UInt128), GetTestNumbers<UInt128>(10),
            @"0|34028236692093846346337460743176821145|68056473384187692692674921486353642290|102084710076281539039012382229530463435|136112946768375385385349842972707284580|170141183460469231731687303715884105725|204169420152563078078024764459060926870|238197656844656924424362225202237748015|272225893536750770770699685945414569160|306254130228844617117037146688591390305" ),
#endif

        (typeof(BigInteger), GetTestNumbers(BigInteger.Parse("-12345678901234567890"), BigInteger.Parse("12345678901234567890"), BigInteger.Parse("2469135780246913578"), (n1, n2) => n1+n2),
            @"-12345678901234567890|-9876543120987654312|-7407407340740740734|-4938271560493827156|-2469135780246913578|0|2469135780246913578|4938271560493827156|7407407340740740734|9876543120987654312|12345678901234567890" ),

#if NET
        (typeof(Half), GetTestNumbers((float)Half.MinValue+(float)Half.MaxValue/10,
            (float)Half.MaxValue-(float)Half.MaxValue/10,
            (float)Half.MaxValue/10, (n1, n2) => n1+n2).Select(f=>(Half)f).ToList(),
            @"-58940|-52400|-45860|-39300|-32750|-26200|-19650|-13100|-6550|-0.004883|6550|13100|19650|26200|32750|39300|45860|52400|58940" ),

        (typeof(Half), GetTestNumbers(1, 0b11_1111_1111, 100, (n1, n2) => n1+n2).Select(i=>
            {
                ushort variable = (ushort)i;
                return System.Runtime.CompilerServices.Unsafe.As<ushort, Half>(ref variable);
            }
        ).ToList(),
            @"5.9604644775390625E-08|6.0200691223144531E-06|1.1980533599853516E-05|1.7940998077392578E-05|2.3901462554931641E-05|2.9861927032470703E-05|3.5822391510009766E-05|4.1782855987548828E-05|4.7743320465087891E-05|5.3703784942626953E-05|5.9664249420166016E-05" ),
#endif
 
        (typeof(float), GetTestNumbers(float.MinValue+float.MaxValue/10, float.MaxValue-float.MaxValue/10, float.MaxValue/10, (n1, n2) => n1+n2),
            @"-3.06254122E+38|-2.72225877E+38|-2.38197633E+38|-2.04169388E+38|-1.70141153E+38|-1.36112918E+38|-1.02084684E+38|-6.8056449E+37|-3.40282144E+37|2.02824096E+31|3.40282549E+37|6.80564896E+37|1.02084724E+38|1.36112959E+38|1.70141183E+38|2.04169428E+38|2.38197673E+38|2.72225918E+38" ),

        (typeof(double), GetTestNumbers(double.MinValue+double.MaxValue/10, double.MaxValue-double.MaxValue/10, double.MaxValue/10, (n1, n2) => n1+n2),
            @"-1.6179238213760842E+308|-1.4381545078898526E+308|-1.2583851944036211E+308|-1.0786158809173895E+308|-8.9884656743115795E+307|-7.190772539449264E+307|-5.3930794045869485E+307|-3.595386269724633E+307|-1.7976931348623173E+307|-1.4968802321510399E+292|1.7976931348623143E+307|3.59538626972463E+307|5.3930794045869455E+307|7.190772539449261E+307|8.9884656743115765E+307|1.0786158809173893E+308|1.2583851944036209E+308|1.4381545078898524E+308|1.617923821376084E+308" ),

        (typeof(decimal), GetTestNumbers(decimal.MinValue+(decimal)0.123456789, decimal.MaxValue-decimal.MaxValue/10, decimal.MaxValue/5, (n1, n2) => n1+n2),
            @"-79228162514264337593543950335|-63382530011411470074835160268|-47536897508558602556126370201|-31691265005705735037417580134|-15845632502852867518708790067|0|15845632502852867518708790067|31691265005705735037417580134|47536897508558602556126370201|63382530011411470074835160268" ),


        (typeof(TimeSpan), GetTestNumbers<long>(10).Select(ticks => new TimeSpan(ticks)).ToList(),
            @"-10675199.02:48:05.4775808|-9607679.04:55:16.9298228|-8540159.07:02:28.3820648|-7472639.09:09:39.8343068|-6405119.11:16:51.2865488|-5337599.13:24:02.7387908|-4270079.15:31:14.1910328|-3202559.17:38:25.6432748|-2135039.19:45:37.0955168|-1067519.21:52:48.5477588|-00:00:00.0000008|1067519.21:52:48.5477572|2135039.19:45:37.0955152|3202559.17:38:25.6432732|4270079.15:31:14.1910312|5337599.13:24:02.7387892|6405119.11:16:51.2865472|7472639.09:09:39.8343052|8540159.07:02:28.3820632|9607679.04:55:16.9298212"),

        (typeof(TimeSpan), Enumerable.Range(1, 7).Select(i => new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
        @"1.02:03:04|2.03:04:05|3.04:05:06|4.05:06:07|5.06:07:08|6.07:08:09|7.08:09:10" ),

        (typeof(TimeSpan),
            Enumerable.Range(1, 7).Select(i => i % 2 == 0 ? TimeSpan.Zero : new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
        @"1.02:03:04|∅|3.04:05:06|∅|5.06:07:08|∅|7.08:09:10"),

        (typeof(TimeSpan?), Enumerable.Range(1, 7).Select(i => i % 3 == 0 ? (TimeSpan?) null : new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
        @"1.02:03:04|2.03:04:05|∅|4.05:06:07|5.06:07:08|∅|7.08:09:10" ),

        (typeof(DateTime?), Enumerable.Range(1, 7).Select(i => i % 3 == 0 ? (DateTime?) null : new DateTime(2000+i, i, i * 2, i*3, i*4, i*5, i*111)).ToList(),
            @"2001-01-02T03:04:05.1110000|2002-02-04T06:08:10.2220000|∅|2004-04-08T12:16:20.4440000|2005-05-10T15:20:25.5550000|∅|2007-07-14T21:28:35.7770000" ),
 
#if NET6_0_OR_GREATER
        (typeof(DateOnly), Enumerable.Range(1, 7).Select(i => new DateOnly(2000+i, i, i * 2)).ToList(),
            @"2001-01-02|2002-02-04|2003-03-06|2004-04-08|2005-05-10|2006-06-12|2007-07-14" ),

        (typeof(TimeOnly), Enumerable.Range(1, 7).Select(i =>  new TimeOnly( i*3, i*4, i*5, i*111)).ToList(),
            @"03:04:05.1110000|06:08:10.2220000|09:12:15.3330000|12:16:20.4440000|15:20:25.5550000|18:24:30.6660000|21:28:35.7770000" ),
#endif

        (typeof(Uri), Enumerable.Range(1, 7).Select(i => new Uri($"http://www.google{i}.com")).ToList(),
            @"http://www.google1.com|http://www.google2.com|http://www.google3.com|http://www.google4.com|http://www.google5.com|http://www.google6.com|http://www.google7.com" ),

        (typeof(decimal[]), Enumerable.Range(1, 7).Select(
                i => i % 3 == 0 ? null : new decimal[] {10 * i, 10 * i + 1}).ToList(),
        @"10\|11|20\|21|∅|40\|41|50\|51|∅|70\|71" ),

        (typeof(float[]), Enumerable.Range(1, 5).Select(
                i => new float[] {10 * i, 10 * i + 1, 10 * i + 2}).ToList(),
        @"10\|11\|12|20\|21\|22|30\|31\|32|40\|41\|42|50\|51\|52" ),

        (typeof(float[][]), Enumerable.Range(1, 1).Select(
                i => new[] {new float[]{ 10 * i, 10 * i + 1 }, [100 * i, 100 * i + 1] }
        ).ToList(),
        @"10\\\|11\|100\\\|101" ),

        (typeof(float[][]), Enumerable.Range(1, 3).Select(
                i => new[] {new float[]{ 10 * i, 10 * i + 1 }, [100 * i, 100 * i + 1] }
        ).ToList(),
        @"10\\\|11\|100\\\|101|20\\\|21\|200\\\|201|30\\\|31\|300\\\|301" ),

        (typeof(List<float>), Enumerable.Range(1, 5).Select(
            i => new List<float> {10 * i, 10 * i + 1, 10 * i + 2}).ToList(),
        @"10\|11\|12|20\|21\|22|30\|31\|32|40\|41\|42|50\|51\|52" ),

        (typeof(IList<float>), Enumerable.Range(1, 5).Select(
            i => (IList<float>)[10 * i, 10 * i + 1, 10 * i + 2]).ToList(),
        @"10\|11\|12|20\|21\|22|30\|31\|32|40\|41\|42|50\|51\|52" ),

        (typeof(bool), Enumerable.Range(1, 8).Select(i => i % 2 == 0).ToList(),
            @"False|True|False|True|False|True|False|True"),

        (typeof(bool), Enumerable.Range(1, 10).Select(i => i % 2 == 0).ToList(),
            @"∅|True|False|True|False|True|False|True|False|True"),

        (typeof(bool?), Enumerable.Range(1, 10).Select(i => i % 3 == 0 ? (bool?)null : (i % 2 == 0)).ToList(),
            @"False|True|∅|True|False|∅|False|True|∅|True"),

        (typeof(Complex), Enumerable.Range(1, 7).Select(i => new Complex(i*1.1, -i*2.2)).ToList(),
            @"(1.1; -2.2)|(2.2; -4.4)|(3.3000000000000003; -6.6000000000000005)|(4.4; -8.8)|(5.5; -11)|(6.6000000000000005; -13.200000000000001)|(7.7000000000000011; -15.400000000000002)" ),

        (typeof(Complex?), Enumerable.Range(1, 7).Select(i => i % 3 == 0 ? (Complex?)null :new Complex(i*1.1, -i*2.2)).ToList(),
            @"(1.1; -2.2)|(2.2; -4.4)|∅|(4.4; -8.8)|(5.5; -11)|∅|(7.7000000000000011; -15.400000000000002)" ),

        (typeof(Point), Enumerable.Range(0, 6).Select(i => new Point(i * 10, i * 20)).ToList(),
            @"∅|10;20|20;40|30;60|40;80|50;100"),

        (typeof(Point?),
            Enumerable.Range(1, 6).Select(i => i % 2 == 0 ? (Point?) null : new Point(i * 10, i * 20)).ToList(),
            @"10;20|∅|30;60|∅|50;100|∅"),

        (typeof(Color), Enumerable.Range(1, 5).Select(i => (Color) i).ToList(),
            @"Red|Blue|Green|4|5"),

        (typeof(Colors), Enumerable.Range(0, 9).Select(i => (Colors) i).ToList(),
            @"None|Red|Blue|RedAndBlue|Green|Red,Green|Blue,  Green|RedAndBlue,Green|8"),

        (typeof(Color?), Enumerable.Range(1, 8).Select(i => i % 2 == 0 ? (Color?) null : (Color) i).ToList(),
            @" Red |∅|Green|∅|5||7|∅"),

        (typeof(Rect),
            Enumerable.Range(0, 4).Select(i => new Rect(i * 10 + 1, i * 10 + 2, i * 10 + 3, i * 10 + 4)).ToList(),
            @"1;2;3;4|11;12;13;14|21;22;23;24|31;32;33;34"),

        (typeof(ThreeLetters),
            Enumerable.Range(-2, 6).Select(i => i < 0
                ? default
                : new ThreeLetters((char) (65 + i + 0), (char) (65 + i + 1), (char) (65 + i + 2))).ToList(),
            @"|∅|ABC|BCD|CDE|DEF"), //∅ == "" == "\0\0\0"

        (typeof(ThreeLetters?),
            Enumerable.Range(0, 7).Select(i =>
                i>0 && i % 3 == 0
                    ? (ThreeLetters?) null
                    : new ThreeLetters((char) (65 + i + 0), (char) (65 + i + 1), (char) (65 + i + 2))).ToList(),
            @"ABC|BCD|CDE|∅|EFG|FGH|∅"),

        (typeof(ThreeElements<float>),
            Enumerable.Range(0, 3).Select(i => new ThreeElements<float>(i+0.5f,i+1.5f,i+2.5f)).ToList(),
            @"0.5,1.5,2.5|1.5,2.5,3.5|2.5,3.5,4.5"),

        (typeof(PairWithFactory<float>),
            Enumerable.Range(0, 3).Select(i => new PairWithFactory<float>(i+0.5f,i+1.5f)).ToList(),
            @"0.5,1.5|1.5,2.5|2.5,3.5"),

        (typeof(HashSet<float>),
            Enumerable.Range(1, 3).Select(i =>
                new HashSet<float>
                {
                    i*30 + 0.5f,
                    i*60 + 0.5f,
                }
            ).ToList(),
            @"30.5\|60.5|60.5\|120.5|90.5\|180.5"),

        (typeof(SortedDictionary<int, float>),
            Enumerable.Range(1, 3).Select(i =>
                new SortedDictionary<int, float>
                {
                    [i*10+0]=i*10 + 0.5f,
                    [i*20+0]=i*20 + 0.5f,
                }
            ).ToList(),
            @"10=10.5;20=20.5|20=20.5;40=40.5|30=30.5;60=60.5"),

        (typeof(ReadOnlyDictionary<int, float>),
            Enumerable.Range(1, 4).Select(i => new ReadOnlyDictionary<int, float>(
                new Dictionary<int, float>()
                {
                    [i*100+0]=i*10 + 0.5f,
                    [i*200+0]=i*20 + 0.5f,
                })
            ).ToList(),
            @"100=10.5;200=20.5|200=20.5;400=40.5|300=30.5;600=60.5|400=40.5;800=80.5"),

        (typeof(Option),
            Enumerable.Range(0, 5).Select(i => new Option((OptionEnum)i)).ToList(),
            @" None|Option1 | Option2 |Option3  | 4"),

        (typeof(Option),
            Enumerable.Range(0, 10).Select(i => new Option((OptionEnum)(i *9 % 10))).ToList(),
            @" None |  9 |  8 |  7 |  6 |  5 |  4 |  Option3 |  Option2 |  Option1"),


        (typeof(SortedDictionary<char, float[]>),
            Enumerable.Range(1, 3).Select(i =>
                new SortedDictionary<char, float[]>()
                {
                    [(char)(65+(i-1)*3+0)] = [i*10 + 0.5f, i*10 + 1.5f, i*10 + 2.5f, i*10 + 3.5f],
                    [(char)(65+(i-1)*3+1)] = [i*100 + 0.5f, i*100 + 1.5f, i*100 + 2.5f, i*100 + 3.5f],
                }
            ).ToList(),
            @"A=10.5\|11.5\|12.5\|13.5;B=100.5\|101.5\|102.5\|103.5|D=20.5\|21.5\|22.5\|23.5;E=200.5\|201.5\|202.5\|203.5|G=30.5\|31.5\|32.5\|33.5;H=300.5\|301.5\|302.5\|303.5"),

        (typeof(KeyValuePair<string, float?>),
            new[]
            {
                new KeyValuePair<string, float?>("PI", 3.14f),
                new KeyValuePair<string, float?>("PI", null),
                new KeyValuePair<string, float?>("", 3.14f),
                new KeyValuePair<string, float?>(null, 3.14f),

                new KeyValuePair<string, float?>("", null),
                new KeyValuePair<string, float?>(null, null),
                new KeyValuePair<string, float?>("", null),

                default,
                new KeyValuePair<string, float?>("", null),
                new KeyValuePair<string, float?>("", 0),
                default
            }.ToList(),
            @"PI=3.14|PI=∅|=3.14|∅=3.14||∅=∅||∅=∅|=∅|=0|∅"),

        (typeof( (TimeSpan, int, float, string, decimal?) ),
            new[]
            {
                (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m),
                (new TimeSpan(31,14,15,9), 3, 3.14f, "Pi", (decimal?)null),
                (TimeSpan.MinValue, 3, 3.14f, "Pi", (decimal?)null),

                (TimeSpan.Zero, 0, 0f, "", 0m),
                (TimeSpan.Zero, 0, 0f, (string)null, (decimal?)null),
                default,
                default,
                default,
            }.ToList(),
            @"(3.14:15:09,3,3.1400001,Pi,3.14)|(31.14:15:09,3,3.1400001,Pi,\∅)|(-10675199.02:48:05.4775808,3,3.1400001,Pi,\∅)|(00:00:00,0,0,,0)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)"),

        (typeof( (TimeSpan, int, float, string, decimal?) ),
            new[]
            {
                (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m),
                (new TimeSpan(31,14,15,9), 3, 3.14f, "Pi", (decimal?)null),
                (TimeSpan.MinValue, 3, 3.14f, "Pi", (decimal?)null),

                (TimeSpan.Zero, 0, 0f, "", 0m),
                (TimeSpan.Zero, 0, 0f, (string)null, (decimal?)null),
                default,
                default,
                (TimeSpan.Zero, 0, 0f, "", (decimal?)null),
            }.ToList(),
            @"(3.14:15:09,3,3.1400001,Pi,3.14)|(31.14:15:09,3,3.1400001,Pi,\∅)|(-10675199.02:48:05.4775808,3,3.1400001,Pi,\∅)|(00:00:00,0,0,,0)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)|∅|"),
    };

    private static ReadOnlyCollection<TNumber> GetTestNumbers<TNumber>(TNumber from, TNumber to, TNumber increment, Func<TNumber, TNumber, TNumber> addFunc)
            where TNumber : struct, IComparable, IComparable<TNumber>, IEquatable<TNumber>, IFormattable
    {
        var result = new List<TNumber>();
        for (var i = from; i.CompareTo(to) <= 0; i = addFunc(i, increment))
            result.Add(i);

        return result.AsReadOnly();

    }

    private static ReadOnlyCollection<TNumber> GetTestNumbers<TNumber>(long divisor)
        where TNumber : struct, IComparable, IComparable<TNumber>, IEquatable<TNumber>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TNumber>
#endif
    {
        var transformer = NumberTransformerCache.Instance.GetNumberHandler<TNumber>();
        var (_, _, min, max) = transformer;

        var div = transformer.FromInt64(divisor);
        var inc = transformer.Div(max, div);

        var from = min;
        var result = new List<TNumber>();
        var to = transformer.Sub(
                    max,
                    transformer.Div(max, div));
        for (var i = from; i.CompareTo(to) <= 0; i = transformer.Add(i, inc))
            result.Add(i);

        return result.AsReadOnly();

    }
}
