using System.Text;
using System.Text.Json;
using ClinicPos.Api.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ClinicPos.Api.Features.Appointments;

public interface IAppointmentEventPublisher
{
    Task PublishCreatedAsync(AppointmentDto appointment, CancellationToken cancellationToken);
}

public class AppointmentEventPublisher(IOptions<RabbitMqOptions> options) : IAppointmentEventPublisher
{
    public async Task PublishCreatedAsync(AppointmentDto appointment, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.Username,
            Password = config.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            eventName = "appointment.created",
            appointment.TenantId,
            appointment.BranchId,
            appointment.PatientId,
            appointment.StartAt,
            appointment.CreatedAt,
            appointment.Id
        });

        var body = Encoding.UTF8.GetBytes(payload);

        await channel.BasicPublishAsync(
            exchange: config.Exchange,
            routingKey: "appointments.created",
            mandatory: false,
            body: body,
            cancellationToken: cancellationToken);
    }
}
