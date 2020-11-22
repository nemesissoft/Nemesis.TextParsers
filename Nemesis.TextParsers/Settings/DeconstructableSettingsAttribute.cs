using System;

namespace Nemesis.TextParsers.Settings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class DeconstructableSettingsAttribute : Attribute
    {
        public const char DEFAULT_DELIMITER = ';';
        public const char DEFAULT_NULL_ELEMENT_MARKER = '∅';
        public const char DEFAULT_ESCAPING_SEQUENCE_START = '\\';
        public const char DEFAULT_START = '(';
        public const char DEFAULT_END = ')';
        public const bool DEFAULT_USE_DECONSTRUCTABLE_EMPTY = true;

        public char Delimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char Start { get; }
        public char End { get; }
        public bool UseDeconstructableEmpty { get; }

        /// <summary>Initialize DeconstructableSettingsAttribute </summary>
        /// <param name="delimiter">Properties text delimiter</param>
        /// <param name="nullElementMarker">Null element marker</param>
        /// <param name="escapingSequenceStart">Escaping sequence start</param>
        /// <param name="start">Starting character. Use default(char)=='\0' to omit this character</param>
        /// <param name="end">End character. Use default(char)=='\0' to omit this character</param>
        /// <param name="useDeconstructableEmpty">When <c>true</c>, a default "empty" instance will be crated from empty string i.e. new Class(empty(parameter1), ..., empty(parameterN)) </param>
        public DeconstructableSettingsAttribute(char delimiter = DEFAULT_DELIMITER,
            char nullElementMarker = DEFAULT_NULL_ELEMENT_MARKER, char escapingSequenceStart = DEFAULT_ESCAPING_SEQUENCE_START,
            char start = DEFAULT_START, char end = DEFAULT_END,
            bool useDeconstructableEmpty = DEFAULT_USE_DECONSTRUCTABLE_EMPTY)
        {
            Delimiter = delimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
            UseDeconstructableEmpty = useDeconstructableEmpty;
        }
    }
}
