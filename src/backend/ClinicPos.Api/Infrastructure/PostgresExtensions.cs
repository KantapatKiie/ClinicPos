using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Infrastructure;

public static class PostgresExtensions
{
    public static bool IsUniqueViolation(this Exception ex)
    {
        return ex is NpgsqlException npgsql && npgsql.SqlState == PostgresErrorCodes.UniqueViolation
               || ex is DbUpdateException dbUpdate && dbUpdate.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true
               || ex.InnerException is not null && ex.InnerException.IsUniqueViolation();
    }
}
