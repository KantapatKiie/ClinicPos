using System.Security.Claims;

namespace ClinicPos.Api.Auth;

public class TenantRequestContext(IHttpContextAccessor contextAccessor)
{
    public Guid? GetTenantFromHeader()
    {
        var tenantValue = contextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();
        return Guid.TryParse(tenantValue, out var tenantId) ? tenantId : null;
    }

    public Guid? GetTenantFromToken()
    {
        var value = contextAccessor.HttpContext?.User.FindFirstValue("tenant_id");
        return Guid.TryParse(value, out var tenantId) ? tenantId : null;
    }

    public bool IsAdmin()
    {
        return string.Equals(
            contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role),
            "Admin",
            StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<Guid> GetBranchIds()
    {
        return contextAccessor.HttpContext?.User.FindAll("branch_id")
            .Select(x => Guid.TryParse(x.Value, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .ToArray() ?? [];
    }

    public bool ValidateTenant(Guid tenantId, out string error)
    {
        var headerTenant = GetTenantFromHeader();
        var tokenTenant = GetTenantFromToken();

        if (headerTenant is null || tokenTenant is null)
        {
            error = "Tenant context is missing";
            return false;
        }

        if (headerTenant != tokenTenant || tenantId != headerTenant.Value)
        {
            error = "Tenant mismatch";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool CanAccessBranch(Guid? branchId)
    {
        if (branchId is null || IsAdmin())
        {
            return true;
        }

        var branchIds = GetBranchIds();
        return branchIds.Contains(branchId.Value);
    }
}
