.class public EnumConverter.Class1
{
	.method public hidebysig static !!TEnum ToEnumIl<
		([mscorlib]System.Enum) TEnum, 
		valuetype .ctor ([mscorlib]System.IComparable, class [mscorlib]System.IComparable`1<!!TUnderlying>, [mscorlib]System.IConvertible, class [mscorlib]System.IEquatable`1<!!TUnderlying>, [mscorlib]System.IFormattable, [mscorlib]System.ValueType) TUnderlying
		> 
	(!!TUnderlying number) cil managed 
	{
		.maxstack 1
        ldarg.0
        ret
	} 

	.method public hidebysig static 
	valuetype [mscorlib]System.DayOfWeek ToEnumIlConcrete 
		(int32 number) cil managed 
	{
		.maxstack 1
		ldarg.0
		ret
	}
}
