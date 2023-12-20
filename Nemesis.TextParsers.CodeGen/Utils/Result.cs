#nullable enable
namespace Nemesis.TextParsers.CodeGen.Utils;

internal readonly struct Result<TValue, TError>
{
    public TValue? Value { get; }
    public TError? Error { get; }

    public bool IsError { get; }

    public bool IsSuccess => !IsError;

    private Result(TValue value)
    {
        Value = value;
        Error = default;
        IsError = false;
    }

    private Result(TError error)
    {
        Value = default;
        Error = error;
        IsError = true;
    }

    public static implicit operator Result<TValue, TError>(TValue result) => new(result);
    public static implicit operator Result<TValue, TError>(TError error) => new(error);

    public TResult Match<TResult>(Func<TValue, TResult> success, Func<TError, TResult> failure) =>
        IsError ? failure(Error!) : success(Value!);

    public override string? ToString() => IsError ? Error?.ToString() : Value?.ToString();
}
