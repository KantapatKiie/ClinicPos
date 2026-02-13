using ClinicPos.Api.Data;
using ClinicPos.Api.Domain;
using ClinicPos.Api.Features.Patients;
using ClinicPos.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ClinicPos.Api.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsOnlyTenantData()
    {
        await using var fixture = await TestFixture.CreateAsync();

        fixture.DbContext.Patients.AddRange(
            new Patient
            {
                Id = Guid.NewGuid(),
                TenantId = fixture.TenantA,
                FirstName = "A",
                LastName = "One",
                PhoneNumber = "1000",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                TenantId = fixture.TenantB,
                FirstName = "B",
                LastName = "Two",
                PhoneNumber = "2000",
                CreatedAt = DateTimeOffset.UtcNow
            });

        await fixture.DbContext.SaveChangesAsync();

        var result = await fixture.Service.ListAsync(fixture.TenantA, null, CancellationToken.None);

        Assert.Single(result);
        Assert.All(result, x => Assert.Equal(fixture.TenantA, x.TenantId));
    }

    [Fact]
    public async Task CreateAsync_DuplicatePhoneInSameTenant_ThrowsDuplicatePhoneException()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var first = new CreatePatientRequest("Jane", "Doe", "0999999999", fixture.TenantA, null);
        await fixture.Service.CreateAsync(first, CancellationToken.None);

        var duplicate = new CreatePatientRequest("Janet", "Doe", "0999999999", fixture.TenantA, null);

        await Assert.ThrowsAsync<DuplicatePhoneException>(async () =>
            await fixture.Service.CreateAsync(duplicate, CancellationToken.None));
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext DbContext { get; }
        public IPatientService Service { get; }
        public Guid TenantA { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public Guid TenantB { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private TestFixture(SqliteConnection connection, AppDbContext dbContext, IPatientService service)
        {
            _connection = connection;
            DbContext = dbContext;
            Service = service;
        }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new AppDbContext(dbOptions);
            await dbContext.Database.EnsureCreatedAsync();

            var distributedCache = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
            var cacheVersionService = new InMemoryTenantCacheVersionService();
            var service = new PatientService(dbContext, distributedCache, cacheVersionService);

            return new TestFixture(connection, dbContext, service);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class InMemoryTenantCacheVersionService : ITenantCacheVersionService
    {
        private readonly Dictionary<Guid, long> _versions = [];

        public Task<string> GetCurrentVersionAsync(Guid tenantId)
        {
            var value = _versions.TryGetValue(tenantId, out var version) ? version : 1;
            return Task.FromResult(value.ToString());
        }

        public Task BumpAsync(Guid tenantId)
        {
            _versions[tenantId] = _versions.TryGetValue(tenantId, out var version) ? version + 1 : 2;
            return Task.CompletedTask;
        }
    }
}
