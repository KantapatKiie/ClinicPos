using System.ComponentModel.DataAnnotations;

namespace ClinicPos.Api.Domain;

public class Tenant
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    public List<Branch> Branches { get; set; } = [];
}

public class Branch
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    public Tenant? Tenant { get; set; }
}

public class UserAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public required string Email { get; set; }

    [MaxLength(200)]
    public required string ApiToken { get; set; }

    public UserRole Role { get; set; }

    public List<UserBranch> UserBranches { get; set; } = [];
}

public class UserBranch
{
    public Guid UserAccountId { get; set; }
    public Guid BranchId { get; set; }

    public UserAccount? UserAccount { get; set; }
    public Branch? Branch { get; set; }
}

public class Patient
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? PrimaryBranchId { get; set; }

    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public required string LastName { get; set; }

    [MaxLength(50)]
    public required string PhoneNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class Appointment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid PatientId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
