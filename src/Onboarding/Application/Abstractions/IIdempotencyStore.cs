namespace Onboarding.Application.Abstractions;

public sealed record IdempotencyEntry(int StatusCode, string Body, string RequestHash);

public interface IIdempotencyStore
{
    Task<IdempotencyEntry?> GetAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task SetAsync(string idempotencyKey, IdempotencyEntry entry, TimeSpan ttl, CancellationToken cancellationToken);
}
