using ClinicPos.Api.Data;
using ClinicPos.Api.Domain;
using ClinicPos.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Features.Appointments;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken);
}

public class DuplicateAppointmentException : Exception;
public class MissingPatientException : Exception;

public class AppointmentService(
    AppDbContext dbContext,
    IAppointmentEventPublisher publisher,
    ITenantCacheVersionService cacheVersionService) : IAppointmentService
{
    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var patientExists = await dbContext.Patients.AnyAsync(
            x => x.Id == request.PatientId && x.TenantId == request.TenantId,
            cancellationToken);

        if (!patientExists)
        {
            throw new MissingPatientException();
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            BranchId = request.BranchId,
            PatientId = request.PatientId,
            StartAt = request.StartAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Appointments.Add(appointment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.IsUniqueViolation())
        {
            throw new DuplicateAppointmentException();
        }

        var dto = new AppointmentDto(
            appointment.Id,
            appointment.TenantId,
            appointment.BranchId,
            appointment.PatientId,
            appointment.StartAt,
            appointment.CreatedAt);

        await publisher.PublishCreatedAsync(dto, cancellationToken);
        await cacheVersionService.BumpAsync(appointment.TenantId);

        return dto;
    }
}
