using System.ComponentModel.DataAnnotations;

namespace ClinicPos.Api.Features.Patients;

public record CreatePatientRequest(
    [property: Required] string FirstName,
    [property: Required] string LastName,
    [property: Required] string PhoneNumber,
    [property: Required] Guid TenantId,
    Guid? PrimaryBranchId);

public record PatientDto(
    Guid Id,
    Guid TenantId,
    Guid? PrimaryBranchId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateTimeOffset CreatedAt);
