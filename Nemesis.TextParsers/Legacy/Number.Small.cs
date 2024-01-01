// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#if NETSTANDARD2_0 || NETFRAMEWORK

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Legacy
{
    internal static partial class Number
    {
        private const NumberStyles INVALID_NUMBER_STYLES = ~(
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign | NumberStyles.AllowParentheses |
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier
            );

        internal static void ValidateParseStyleInteger(NumberStyles style)
        {
            // Check for undefined flags
            if ((style & INVALID_NUMBER_STYLES) != 0)
            {
                throw new ArgumentException("Invalid Number Styles", nameof(style));
            }
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // Check for hex number
                if ((style & ~NumberStyles.HexNumber) != 0)
                {
                    throw new ArgumentException("Invalid Hex Style");
                }
            }
        }
    }

    internal static class ByteParser
    {
        private static byte MinValue => byte.MinValue;
        private static byte MaxValue => byte.MaxValue;

        public static byte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        {
            Number.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static byte Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            int i;
            try
            {
                i = Number.ParseInt32(s, style, info);
            }
            catch (OverflowException)
            {
                throw Number.GetOverflowException(typeof(byte));
            }

            return i < MinValue || i > MaxValue ? throw Number.GetOverflowException(typeof(byte)) : (byte)i;
        }

        public static bool TryParse(ReadOnlySpan<char> s, out byte result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out byte result)
        {
            Number.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out Byte result)
        {
            result = 0;
            if (!Number.TryParseInt32(s, style, info, out var i))
            {
                return false;
            }
            if (i < MinValue || i > MaxValue)
            {
                return false;
            }
            result = (byte)i;
            return true;
        }

    }

    internal static class SByteParser
    {
        private static sbyte MinValue => sbyte.MinValue;
        private static sbyte MaxValue => sbyte.MaxValue;

        public static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        {
            Number.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            int i;
            try
            {
                i = Number.ParseInt32(s, style, info);
            }
            catch (OverflowException)
            {
                throw Number.GetOverflowException(typeof(sbyte));
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // We are parsing a hexadecimal number
                if ((i < 0) || i > Byte.MaxValue)
                {
                    throw Number.GetOverflowException(typeof(sbyte));
                }
                return (sbyte)i;
            }

            return i < MinValue || i > MaxValue ? throw Number.GetOverflowException(typeof(sbyte)) : (sbyte)i;
        }

        public static bool TryParse(ReadOnlySpan<char> s, out sbyte result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out sbyte result)
        {
            Number.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out SByte result)
        {
            result = 0;
            if (!Number.TryParseInt32(s, style, info, out var i))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // We are parsing a hexadecimal number
                if ((i < 0) || i > Byte.MaxValue)
                {
                    return false;
                }
                result = (sbyte)i;
                return true;
            }

            if (i < MinValue || i > MaxValue)
            {
                return false;
            }
            result = (sbyte)i;
            return true;
        }

    }

    internal static class Int16Parser
    {
        private static short MinValue => short.MinValue;
        private static short MaxValue => short.MaxValue;

        public static short Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        {
            Number.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static short Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            int i;
            try
            {
                i = Number.ParseInt32(s, style, info);
            }
            catch (OverflowException)
            {
                throw Number.GetOverflowException(typeof(short));
            }

            // We need this check here since we don't allow signs to specified in hex numbers. So we fix up the result
            // for negative numbers
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // We are parsing a hexadecimal number
                if ((i < 0) || (i > UInt16.MaxValue))
                {
                    throw Number.GetOverflowException(typeof(short));
                }
                return (short)i;
            }

            return i < MinValue || i > MaxValue ? throw Number.GetOverflowException(typeof(short)) : (short)i;
        }


        public static bool TryParse(ReadOnlySpan<char> s, out short result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out short result)
        {
            Number.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out Int16 result)
        {
            result = 0;
            if (!Number.TryParseInt32(s, style, info, out var i))
            {
                return false;
            }

            // We need this check here since we don't allow signs to specified in hex numbers. So we fix up the result
            // for negative numbers
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // We are parsing a hexadecimal number
                if ((i < 0) || i > UInt16.MaxValue)
                {
                    return false;
                }
                result = (Int16)i;
                return true;
            }

            if (i < MinValue || i > MaxValue)
            {
                return false;
            }
            result = (Int16)i;
            return true;
        }

    }

    internal static class UInt16Parser
    {
#pragma warning disable IDE0051 // Remove unused private members
        private static ushort MinValue => ushort.MinValue;
#pragma warning restore IDE0051 // Remove unused private members
        private static ushort MaxValue => ushort.MaxValue;

        public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
        {
            Number.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static ushort Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            uint i;
            try
            {
                i = Number.ParseUInt32(s, style, info);
            }
            catch (OverflowException)
            {
                throw Number.GetOverflowException(typeof(ushort));
            }

            return i > MaxValue ? throw Number.GetOverflowException(typeof(ushort)) : (ushort)i;
        }

        public static bool TryParse(ReadOnlySpan<char> s, out ushort result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out ushort result)
        {
            Number.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out UInt16 result)
        {
            result = 0;
            if (!Number.TryParseUInt32(s, style, info, out var i))
            {
                return false;
            }
            if (i > MaxValue)
            {
                return false;
            }
            result = (UInt16)i;
            return true;
        }

    }
}

#endif