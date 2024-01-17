#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Nemesis.TextParsers.CodeGen.Utils;

internal readonly struct Result<TValue, TError> : IEquatable<Result<TValue, TError>>
{
    private readonly State _state;
    public TValue? Value { get; }
    public TError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => _state == State.Success;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => _state == State.Error;

    public bool IsNone => _state == State.None;

    private Result(TValue value)
    {
        Value = value;
        Error = default;

        _state = State.Success;
    }

    private Result(TError error)
    {
        Value = default;
        Error = error;

        _state = State.Error;
    }

    public Result()
    {
        Value = default;
        Error = default;

        _state = State.None;
    }

    public static Result<TValue, TError> None() => new();


    public override bool Equals(object? obj) => obj is Result<TValue, TError> result && Equals(result);

    public bool Equals(Result<TValue, TError> other) =>
        _state == other._state && _state switch
        {
            State.Success => EqualityComparer<TValue?>.Default.Equals(Value, other.Value),
            State.Error => EqualityComparer<TError?>.Default.Equals(Error, other.Error),
            State.None => true,
            _ => throw new NotSupportedException($"State {_state} is not supported")
        };

    public override int GetHashCode() => IsSuccess ? Value.GetHashCode() : (IsError ? Error.GetHashCode() : 0);


    public static implicit operator Result<TValue, TError>(TValue result) => new(result);
    public static implicit operator Result<TValue, TError>(TError error) => new(error);

    public static bool operator ==(Result<TValue, TError> left, Result<TValue, TError> right) => left.Equals(right);
    public static bool operator !=(Result<TValue, TError> left, Result<TValue, TError> right) => !(left == right);

    /*public TResult Match<TResult>(Func<TValue, TResult> success, Func<TError, TResult> failure) =>
        IsError ? failure(Error!) : success(Value!);*/

    public void Invoke(Action<TValue> success, Action<TError>? failure = null)
    {
        if (IsSuccess) success(Value);
        else if (IsError) failure?.Invoke(Error);
    }

    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        if (IsSuccess)
        {
            value = Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public bool TryGetError([NotNullWhen(true)] out TError? error)
    {
        if (IsError)
        {
            error = Error;
            return true;
        }
        else
        {
            error = default;
            return false;
        }
    }

    public override string? ToString() => _state switch
    {
        State.Success => Value?.ToString(),
        State.Error => Error?.ToString(),
        State.None => "<None>",
        _ => throw new NotSupportedException($"State {_state} is not supported")
    };

    enum State : byte { None, Success, Error }
}
