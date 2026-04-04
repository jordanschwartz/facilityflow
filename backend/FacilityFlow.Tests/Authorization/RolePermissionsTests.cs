using FacilityFlow.Core.Authorization;
using FacilityFlow.Core.Enums;

namespace FacilityFlow.Tests.Authorization;

public class RolePermissionsTests
{
    [Fact]
    public void Admin_HasFullAccess()
    {
        var permissions = RolePermissions.GetPermissions(UserRole.Admin);
        Assert.Contains(Permission.FullAccess, permissions);
    }

    [Fact]
    public void Operator_HasSpecificPermissions()
    {
        var permissions = RolePermissions.GetPermissions(UserRole.Operator);
        Assert.Contains(Permission.CreateWorkOrders, permissions);
        Assert.Contains(Permission.EditWorkOrders, permissions);
        Assert.Contains(Permission.SendWorkOrders, permissions);
        Assert.Contains(Permission.ManageVendors, permissions);
        Assert.Contains(Permission.CreateProposals, permissions);
        Assert.Contains(Permission.SendInvoices, permissions);
        Assert.Contains(Permission.MarkInvoicePaid, permissions);
        Assert.DoesNotContain(Permission.FullAccess, permissions);
    }

    [Fact]
    public void FullAccess_GrantsAnyPermission()
    {
        // Admin has FullAccess, so HasPermission should return true for any permission
        foreach (var permission in Enum.GetValues<Permission>())
        {
            Assert.True(RolePermissions.HasPermission(UserRole.Admin, permission),
                $"Admin should have {permission} via FullAccess");
        }
    }

    [Fact]
    public void Operator_DoesNotHaveManageUsers()
    {
        Assert.False(RolePermissions.HasPermission(UserRole.Operator, Permission.ManageUsers));
    }

    [Fact]
    public void Client_HasNoPermissions()
    {
        var permissions = RolePermissions.GetPermissions(UserRole.Client);
        Assert.Empty(permissions);
    }

    [Fact]
    public void Vendor_HasNoPermissions()
    {
        var permissions = RolePermissions.GetPermissions(UserRole.Vendor);
        Assert.Empty(permissions);
    }

    [Fact]
    public void HasPermission_ReturnsTrueForAssignedPermission()
    {
        Assert.True(RolePermissions.HasPermission(UserRole.Operator, Permission.CreateWorkOrders));
    }

    [Fact]
    public void HasPermission_ReturnsFalseForUnassignedPermission()
    {
        Assert.False(RolePermissions.HasPermission(UserRole.Operator, Permission.ManageSettings));
    }
}
