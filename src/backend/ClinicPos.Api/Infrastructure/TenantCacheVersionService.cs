using StackExchange.Redis;

namespace ClinicPos.Api.Infrastructure;

public interface ITenantCacheVersionService
{
    Task<string> GetCurrentVersionAsync(Guid tenantId);
    Task BumpAsync(Guid tenantId);
}

public class TenantCacheVersionService(IConnectionMultiplexer redis) : ITenantCacheVersionService
{
    public async Task<string> GetCurrentVersionAsync(Guid tenantId)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(CacheKeys.PatientListVersion(tenantId));
        return value.HasValue ? value.ToString() : "1";
    }

    public async Task BumpAsync(Guid tenantId)
    {
        var db = redis.GetDatabase();
        await db.StringIncrementAsync(CacheKeys.PatientListVersion(tenantId));
    }
}
