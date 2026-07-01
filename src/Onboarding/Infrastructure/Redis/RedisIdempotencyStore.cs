using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Onboarding.Application.Abstractions;
using StackExchange.Redis;

namespace Onboarding.Infrastructure.Redis;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer connection) : IIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IdempotencyEntry?> GetAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var value = await connection.GetDatabase().StringGetAsync(GetRedisKey(idempotencyKey));

        return value.HasValue
            ? JsonSerializer.Deserialize<IdempotencyEntry>((string)value!, JsonOptions)
            : null;
    }

    public Task SetAsync(string idempotencyKey, IdempotencyEntry entry, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var value = JsonSerializer.Serialize(entry, JsonOptions);
        return connection.GetDatabase().StringSetAsync(GetRedisKey(idempotencyKey), value, ttl);
    }

    private static string GetRedisKey(string idempotencyKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey));
        return $"idempotency:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
