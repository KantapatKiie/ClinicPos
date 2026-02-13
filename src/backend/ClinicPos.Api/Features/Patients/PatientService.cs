using System.Text.Json;
using ClinicPos.Api.Data;
using ClinicPos.Api.Domain;
using ClinicPos.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ClinicPos.Api.Features.Patients;

public interface IPatientService
{
    Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PatientDto>> ListAsync(Guid tenantId, Guid? branchId, CancellationToken cancellationToken);
}

public class DuplicatePhoneException : Exception;

public class PatientService(
    AppDbContext dbContext,
    IDistributedCache cache,
    ITenantCacheVersionService cacheVersionService) : IPatientService
{
    public async Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var entity = new Patient
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            PrimaryBranchId = request.PrimaryBranchId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Patients.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.IsUniqueViolation())
        {
            throw new DuplicatePhoneException();
        }

        await cacheVersionService.BumpAsync(entity.TenantId);

        return new PatientDto(
            entity.Id,
            entity.TenantId,
            entity.PrimaryBranchId,
            entity.FirstName,
            entity.LastName,
            entity.PhoneNumber,
            entity.CreatedAt);
    }

    public async Task<IReadOnlyList<PatientDto>> ListAsync(Guid tenantId, Guid? branchId, CancellationToken cancellationToken)
    {
        var safeVersion = await cacheVersionService.GetCurrentVersionAsync(tenantId);
        var key = CacheKeys.PatientList(tenantId, safeVersion, branchId);

        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return JsonSerializer.Deserialize<List<PatientDto>>(cached) ?? [];
        }

        IQueryable<Patient> query = dbContext.Patients.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (branchId.HasValue)
        {
            query = query.Where(x => x.PrimaryBranchId == branchId);
        }

        var list = await query
            .Select(x => new PatientDto(
                x.Id,
                x.TenantId,
                x.PrimaryBranchId,
                x.FirstName,
                x.LastName,
                x.PhoneNumber,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var ordered = list.OrderByDescending(x => x.CreatedAt).ToList();

        var payload = JsonSerializer.Serialize(ordered);
        await cache.SetStringAsync(key, payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);

        return ordered;
    }

}
