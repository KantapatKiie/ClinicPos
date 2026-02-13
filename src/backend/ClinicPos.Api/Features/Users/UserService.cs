using ClinicPos.Api.Data;
using ClinicPos.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Features.Users;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto?> AssignRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken);
    Task<UserDto?> AssignBranchesAsync(Guid userId, IReadOnlyList<Guid> branchIds, CancellationToken cancellationToken);
}

public class UserService(AppDbContext dbContext) : IUserService
{
    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role,
            ApiToken = $"tok_{Guid.NewGuid():N}"
        };

        foreach (var branchId in request.BranchIds.Distinct())
        {
            user.UserBranches.Add(new UserBranch { UserAccountId = user.Id, BranchId = branchId });
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto?> AssignRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.Include(x => x.UserBranches).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Role = role;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    public async Task<UserDto?> AssignBranchesAsync(Guid userId, IReadOnlyList<Guid> branchIds, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.Include(x => x.UserBranches).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        dbContext.UserBranches.RemoveRange(user.UserBranches);
        user.UserBranches.Clear();

        foreach (var branchId in branchIds.Distinct())
        {
            user.UserBranches.Add(new UserBranch { UserAccountId = user.Id, BranchId = branchId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    private static UserDto ToDto(UserAccount user)
    {
        return new UserDto(
            user.Id,
            user.TenantId,
            user.Email,
            user.Role,
            user.UserBranches.Select(x => x.BranchId).ToArray(),
            user.ApiToken);
    }
}
