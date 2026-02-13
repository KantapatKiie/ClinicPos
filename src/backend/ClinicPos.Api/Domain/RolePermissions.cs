namespace ClinicPos.Api.Domain;

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<UserRole, HashSet<string>> Matrix = new Dictionary<UserRole, HashSet<string>>
    {
        [UserRole.Admin] =
        [
            Permissions.CreatePatients,
            Permissions.ViewPatients,
            Permissions.CreateAppointments,
            Permissions.ManageUsers
        ],
        [UserRole.User] =
        [
            Permissions.CreatePatients,
            Permissions.ViewPatients,
            Permissions.CreateAppointments
        ],
        [UserRole.Viewer] =
        [
            Permissions.ViewPatients
        ]
    };

    public static bool HasPermission(UserRole role, string permission) =>
        Matrix.TryGetValue(role, out var permissions) && permissions.Contains(permission);
}
