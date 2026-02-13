using System.ComponentModel.DataAnnotations;

namespace ClinicPos.Api.Features.Appointments;

public record CreateAppointmentRequest(
    [property: Required] Guid TenantId,
    [property: Required] Guid BranchId,
    [property: Required] Guid PatientId,
    [property: Required] DateTimeOffset StartAt);

public record AppointmentDto(
    Guid Id,
    Guid TenantId,
    Guid BranchId,
    Guid PatientId,
    DateTimeOffset StartAt,
    DateTimeOffset CreatedAt);
