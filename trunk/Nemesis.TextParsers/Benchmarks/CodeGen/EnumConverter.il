//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern netstandard
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )                         // .{...-.Q
  .ver 2:0:0:0
}
.assembly EnumConverter
{
  .custom instance void [netstandard]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [netstandard]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [netstandard]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [netstandard]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .custom instance void [netstandard]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 19 2E 4E 45 54 53 74 61 6E 64 61 72 64 2C   // ....NETStandard,
                                                                                                           56 65 72 73 69 6F 6E 3D 76 32 2E 30 01 00 54 0E   // Version=v2.0..T.
                                                                                                           14 46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61   // .FrameworkDispla
                                                                                                           79 4E 61 6D 65 00 )                               // yName.
  .custom instance void [netstandard]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 0D 45 6E 75 6D 43 6F 6E 76 65 72 74 65 72   // ...EnumConverter
                                                                                                   00 00 ) 
  .custom instance void [netstandard]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 05 44 65 62 75 67 00 00 )                   // ...Debug..
  .custom instance void [netstandard]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [netstandard]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [netstandard]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 0D 45 6E 75 6D 43 6F 6E 76 65 72 74 65 72   // ...EnumConverter
                                                                                                   00 00 ) 
  .custom instance void [netstandard]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 0D 45 6E 75 6D 43 6F 6E 76 65 72 74 65 72   // ...EnumConverter
                                                                                                 00 00 ) 
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module EnumConverter.dll
// MVID: {9A5AE3CF-2F0B-4EB5-BA96-FE29FA1C82FA}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x019B0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit EnumConverter.Convert
       extends [netstandard]System.Object
{
  // method Convert::ToEnumIl forwardref removed for IL import

  // method Convert::ToEnumIlConcrete forwardref removed for IL import

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [netstandard]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Convert::.ctor
  
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

} // end of class EnumConverter.Convert


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file C:\Users\micha\source\repos\EnumConverter\obj\Debug\netstandard2.0\EnumConverter.res
