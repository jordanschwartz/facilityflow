using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Authorization;

public static class RolePermissions
{
    private static readonly Dictionary<UserRole, HashSet<Permission>> Permissions = new()
    {
        [UserRole.Admin] = new() { Permission.FullAccess },
        [UserRole.Operator] = new()
        {
            Permission.CreateWorkOrders,
            Permission.EditWorkOrders,
            Permission.SendWorkOrders,
            Permission.ManageVendors,
            Permission.CreateProposals,
            Permission.SendInvoices,
            Permission.MarkInvoicePaid
        }
    };

    public static HashSet<Permission> GetPermissions(UserRole role)
        => Permissions.TryGetValue(role, out var perms) ? perms : new();

    public static bool HasPermission(UserRole role, Permission permission)
        => GetPermissions(role).Contains(Permission.FullAccess) || GetPermissions(role).Contains(permission);
}
