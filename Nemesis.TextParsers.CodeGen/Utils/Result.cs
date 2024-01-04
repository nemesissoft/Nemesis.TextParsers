#nullable enable
namespace Nemesis.TextParsers.CodeGen.Utils;

internal readonly struct Result<TValue, TError>
{
    private readonly State _state;
    private readonly TValue? _value;
    private readonly TError? _error;

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Value can only be retrieved in 'Success' state");

    public bool IsSuccess => _state == State.Success;
    public bool IsError => _state == State.Error;
    public bool IsNone => _state == State.None;

    private Result(TValue value)
    {
        _value = value;
        _error = default;

        _state = State.Success;
    }

    private Result(TError error)
    {
        _value = default;
        _error = error;

        _state = State.Error;
    }

    public Result()
    {
        _value = default;
        _error = default;

        _state = State.None;
    }

    public static Result<TValue, TError> None() => new();

    public static implicit operator Result<TValue, TError>(TValue result) => new(result);
    public static implicit operator Result<TValue, TError>(TError error) => new(error);

    /*public TResult Match<TResult>(Func<TValue, TResult> success, Func<TError, TResult> failure) =>
        IsError ? failure(Error!) : success(Value!);*/

    public void Invoke(Action<TValue> success, Action<TError>? failure = null)
    {
        if (IsSuccess) success(_value!);
        else if (IsError) failure?.Invoke(_error!);
    }

    public override string? ToString() => _state switch
    {
        State.Success => _value?.ToString(),
        State.Error => _error?.ToString(),
        State.None => "<None>",
        _ => throw new NotSupportedException($"State {_state} is not supported")
    };

    enum State : byte { None, Success, Error }
}
