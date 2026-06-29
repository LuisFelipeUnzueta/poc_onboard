namespace Onboarding.Domain.Common;

public class Result
{
    protected Result(bool isSuccess, DomainError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public DomainError? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(DomainError error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(T? value, bool isSuccess, DomainError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, null);
    public new static Result<T> Failure(DomainError error) => new(default, false, error);
}
