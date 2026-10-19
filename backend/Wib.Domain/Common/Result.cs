namespace Wib.Domain.Common;

public class Result
{
    private readonly DomainError? _error;

    private protected Result(DomainError? error)
    {
        _error = error;
    }

    public static Result Success { get; } = new(null);

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;

    public DomainError Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<DomainError, TOut> onError) =>
        _error is null ? onSuccess() : onError(_error);

    public static implicit operator Result(DomainError error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null)
    {
        _value = value;
    }

    private Result(DomainError error) : base(error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value: {Error.Code}.");

    public TOut Match<TOut>(Func<T, TOut> onValue, Func<DomainError, TOut> onError) =>
        IsSuccess ? onValue(_value!) : onError(Error);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(DomainError error) => new(error);
}
