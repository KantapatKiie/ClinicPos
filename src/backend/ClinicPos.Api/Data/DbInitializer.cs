using ClinicPos.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Data;

public class DbInitializer(IServiceScopeFactory scopeFactory, ILogger<DbInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var branchAId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var branchBId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var tenant = new Tenant { Id = tenantId, Name = "Demo Clinic" };
        var branchA = new Branch { Id = branchAId, TenantId = tenantId, Name = "Bangkok Branch" };
        var branchB = new Branch { Id = branchBId, TenantId = tenantId, Name = "Chiang Mai Branch" };

        var admin = new UserAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "admin@demo.local",
            ApiToken = "admin-token",
            Role = UserRole.Admin,
            UserBranches =
            [
                new UserBranch { BranchId = branchAId },
                new UserBranch { BranchId = branchBId }
            ]
        };

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "user@demo.local",
            ApiToken = "user-token",
            Role = UserRole.User,
            UserBranches =
            [
                new UserBranch { BranchId = branchAId }
            ]
        };

        var viewer = new UserAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "viewer@demo.local",
            ApiToken = "viewer-token",
            Role = UserRole.Viewer,
            UserBranches =
            [
                new UserBranch { BranchId = branchAId }
            ]
        };

        dbContext.Tenants.Add(tenant);
        dbContext.Branches.AddRange(branchA, branchB);
        dbContext.Users.AddRange(admin, user, viewer);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Database seeded with demo tenant and users");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
