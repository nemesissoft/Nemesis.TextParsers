// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NETSTANDARD2_0 || NETFRAMEWORK

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Legacy
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static partial class Number
    {
        private const int NumberMaxDigits = 50; // needs to == NUMBER_MAXDIGITS in coreclr's src/classlibnative/bcltype/number.h.

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe ref struct NumberBuffer // needs to match layout of NUMBER in coreclr's src/classlibnative/bcltype/number.h
        {
            public int precision;
            public int scale;
            private int _sign;
            private DigitsAndNullTerminator _digits;
            private char* _allDigits;

            public bool sign { get => _sign != 0; set => _sign = value ? 1 : 0; }
            public char* digits => (char*)Unsafe.AsPointer(ref _digits);

            [StructLayout(LayoutKind.Sequential, Size = (NumberMaxDigits + 1) * sizeof(char))]
            private struct DigitsAndNullTerminator { }
        }
    }
}

#endif