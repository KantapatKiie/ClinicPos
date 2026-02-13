using System.ComponentModel.DataAnnotations;
using ClinicPos.Api.Domain;

namespace ClinicPos.Api.Features.Users;

public record CreateUserRequest(
    [property: Required] Guid TenantId,
    [property: Required] string Email,
    [property: Required] UserRole Role,
    [property: Required] IReadOnlyList<Guid> BranchIds);

public record AssignRoleRequest([property: Required] UserRole Role);

public record AssignBranchesRequest([property: Required] IReadOnlyList<Guid> BranchIds);

public record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    UserRole Role,
    IReadOnlyList<Guid> BranchIds,
    string ApiToken);
