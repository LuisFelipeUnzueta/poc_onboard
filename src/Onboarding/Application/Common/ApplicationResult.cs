namespace Onboarding.Application.Common;

public sealed class ApplicationResult<T>
{
    private ApplicationResult(T? value, ApplicationError? error, bool idempotencyReplayed)
    {
        Value = value;
        Error = error;
        IdempotencyReplayed = idempotencyReplayed;
    }

    public T? Value { get; }
    public ApplicationError? Error { get; }
    public bool IdempotencyReplayed { get; }
    public bool IsSuccess => Error is null;

    public static ApplicationResult<T> Success(T value, bool idempotencyReplayed = false) =>
        new(value, null, idempotencyReplayed);

    public static ApplicationResult<T> Failure(ApplicationError error) =>
        new(default, error, false);
}
