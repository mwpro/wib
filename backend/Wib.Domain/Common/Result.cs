namespace Wib.Domain.Common;

public sealed class Result
{
    private static readonly Result SuccessResult = new(null);

    private readonly DomainError? _error;

    private Result(DomainError? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;

    public DomainError Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => SuccessResult;

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<DomainError, TOut> onError) =>
        _error is null ? onSuccess() : onError(_error);

    public static implicit operator Result(DomainError error) => new(error);
}

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly DomainError? _error;

    private Result(T value)
    {
        _value = value;
    }

    private Result(DomainError error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;

    public T Value => _error is null
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value: {_error.Code}.");

    public DomainError Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public TOut Match<TOut>(Func<T, TOut> onValue, Func<DomainError, TOut> onError) =>
        _error is null ? onValue(_value!) : onError(_error);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(DomainError error) => new(error);
}
