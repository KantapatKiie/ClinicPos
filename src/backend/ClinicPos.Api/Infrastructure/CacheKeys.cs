namespace ClinicPos.Api.Infrastructure;

public static class CacheKeys
{
    public static string PatientListVersion(Guid tenantId) => $"tenant:{tenantId}:patients:version";

    public static string PatientList(Guid tenantId, string version, Guid? branchId) =>
        $"tenant:{tenantId}:patients:list:{(branchId.HasValue ? branchId.Value.ToString() : "all")}:v:{version}";
}
