namespace Nemesis.TextParsers.Utils;

public sealed class TupleHelper : IEquatable<TupleHelper>
{
    #region Init
    public char TupleDelimiter { get; }
    public char NullElementMarker { get; }
    public char EscapingSequenceStart { get; }
    public char? TupleStart { get; }
    public char? TupleEnd { get; }

    public TupleHelper(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char? tupleStart, char? tupleEnd)
    {
        if (tupleDelimiter == nullElementMarker ||
            tupleDelimiter == escapingSequenceStart ||
            tupleDelimiter == tupleStart ||
            tupleDelimiter == tupleEnd ||

            nullElementMarker == escapingSequenceStart ||
            nullElementMarker == tupleStart ||
            nullElementMarker == tupleEnd ||

            escapingSequenceStart == tupleStart ||
            escapingSequenceStart == tupleEnd
        )
            throw new ArgumentException($@"{nameof(TupleHelper)} requires unique characters to be used for parsing/formatting purposes. Start ('{tupleStart}') and end ('{tupleEnd}') can be equal to each other
Passed parameters: 
{nameof(tupleDelimiter)} = '{tupleDelimiter}'
{nameof(nullElementMarker)} = '{nullElementMarker}'
{nameof(escapingSequenceStart)} = '{escapingSequenceStart}'
{nameof(tupleStart)} = '{tupleStart}'
{nameof(tupleEnd)} = '{tupleEnd}'");

        TupleDelimiter = tupleDelimiter;
        NullElementMarker = nullElementMarker;
        EscapingSequenceStart = escapingSequenceStart;
        TupleStart = tupleStart;
        TupleEnd = tupleEnd;
    }
    #endregion


    #region Formatting
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartFormat(ref ValueSequenceBuilder<char> accumulator)
    {
        if (TupleStart is { } c)
            accumulator.Append(c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndFormat(ref ValueSequenceBuilder<char> accumulator)
    {
        if (TupleEnd is { } c)
            accumulator.Append(c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDelimiter(ref ValueSequenceBuilder<char> accumulator) => accumulator.Append(TupleDelimiter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
    {
        string elementText = formatter.Format(element);
        if (elementText == null)
            accumulator.Append(NullElementMarker);
        else
        {
            foreach (char c in elementText)
            {
                if (c == EscapingSequenceStart || c == NullElementMarker || c == TupleDelimiter)
                    accumulator.Append(EscapingSequenceStart);
                accumulator.Append(c);
            }
        }
    }
    #endregion


    #region Parsing
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TokenSequence<char>.TokenSequenceEnumerator ParseStart(ReadOnlySpan<char> input, byte arity, string typeName = null)
    {
        input = UnParenthesize(input);

        var tokens = input.Tokenize(TupleDelimiter, EscapingSequenceStart, false);
        var enumerator = tokens.GetEnumerator();

        if (!enumerator.MoveNext())
            throw new ArgumentException($@"{typeName ?? "Tuple"} of arity={arity} separated by '{TupleDelimiter}' was not found");

        return enumerator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> UnParenthesize(ReadOnlySpan<char> span, string typeName = null)
    {
        if (TupleStart is null && TupleEnd is null)
            return span;

        int minLength = (TupleStart.HasValue ? 1 : 0) + (TupleEnd.HasValue ? 1 : 0);
        if (span.Length < minLength) throw GetStateException(span.ToString(), TupleStart, TupleEnd, typeName);

        int start = 0;

        if (TupleStart.HasValue)
        {
            for (; start < span.Length; start++)
                if (!char.IsWhiteSpace(span[start]))
                    break;

            bool startsWithChar = start < span.Length && span[start] == TupleStart.Value;
            if (!startsWithChar) throw GetStateException(span.ToString(), TupleStart, TupleEnd, typeName);

            ++start;
        }


        int end = span.Length - 1;

        if (TupleEnd.HasValue)
        {
            for (; end > start; end--)
                if (!char.IsWhiteSpace(span[end]))
                    break;

            bool endsWithChar = end > 0 && span[end] == TupleEnd.Value;
            if (!endsWithChar) throw GetStateException(span.ToString(), TupleStart, TupleEnd, typeName);

            --end;
        }

        return span.Slice(start, end - start + 1);

        static Exception GetStateException(string text, char? start, char? end, string typeName) => new ArgumentException(
                 $@"{typeName ?? "Tuple"} representation has to start with '{(start is { } c1 ? c1.ToString() : "<nothing>")}' and end with '{(end is { } c2 ? c2.ToString() : "<nothing>")}' optionally lead in the beginning or trailed in the end by whitespace.
These requirements were not met in:
'{text ?? "<NULL>"}'");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ParseNext(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte index, string typeName = null)
    {
        static string ToOrdinal(byte number) =>
            number % 100 is <= 13 and >= 11
                ? $"{number}th"
                : (number % 10) switch
                {
                    1 => $"{number}st",
                    2 => $"{number}nd",
                    3 => $"{number}rd",
                    _ => $"{number}th",
                };

        var current = enumerator.Current;
        if (!enumerator.MoveNext())
            throw new ArgumentException($"{typeName ?? "Tuple"}: {ToOrdinal(index)} element was not found after '{current.ToString()}'");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ParseEnd(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte arity, string typeName = null)
    {
        if (enumerator.MoveNext())
        {
            var remaining = enumerator.Current.ToString();
            throw new ArgumentException($@"{typeName ?? "Tuple"} of arity={arity} separated by '{TupleDelimiter}' cannot have more than {arity} elements: '{remaining}'");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TElement ParseElement<TElement>(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, ISpanParser<TElement> parser)
    {
        ReadOnlySpan<char> input = enumerator.Current;
        var unescapedInput = input.UnescapeCharacter(EscapingSequenceStart, TupleDelimiter);

        if (unescapedInput.Length == 1 && unescapedInput[0].Equals(NullElementMarker))
            return default;
        else
        {
            unescapedInput = unescapedInput.UnescapeCharacter
                    (EscapingSequenceStart, NullElementMarker, EscapingSequenceStart);

            return parser.Parse(unescapedInput);
        }
    }

    #endregion


    #region Object helpers
    public override string ToString() =>
        $"{TupleStart}Item1{TupleDelimiter}Item2{TupleDelimiter}…{TupleDelimiter}ItemN{TupleEnd} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

    public bool Equals(TupleHelper other) =>
        other is not null && (ReferenceEquals(this, other) ||
            TupleDelimiter == other.TupleDelimiter && NullElementMarker == other.NullElementMarker &&
            EscapingSequenceStart == other.EscapingSequenceStart &&
            TupleStart == other.TupleStart && TupleEnd == other.TupleEnd);

    public override bool Equals(object obj) =>
        ReferenceEquals(this, obj) || obj is TupleHelper other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = TupleDelimiter.GetHashCode();
            hashCode = (hashCode * 397) ^ NullElementMarker.GetHashCode();
            hashCode = (hashCode * 397) ^ EscapingSequenceStart.GetHashCode();
            hashCode = (hashCode * 397) ^ TupleStart.GetHashCode();
            hashCode = (hashCode * 397) ^ TupleEnd.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(TupleHelper left, TupleHelper right) => Equals(left, right);

    public static bool operator !=(TupleHelper left, TupleHelper right) => !Equals(left, right);

    #endregion
}
