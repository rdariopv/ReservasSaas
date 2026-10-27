using FluentAssertions;
using ReservasSaaS.Domain.Enums;
using ReservasSaaS.Domain.Security;
using Xunit;

namespace ReservasSaaS.UnitTests.Security;

public class RolePermissionsMatrixTests
{
    [Theory]
    [InlineData(CompanyRole.Owner, Permissions.BillingManage, true)]
    [InlineData(CompanyRole.Owner, Permissions.CompanyDelete, true)]
    [InlineData(CompanyRole.Owner, Permissions.MembersRemove, true)]
    [InlineData(CompanyRole.Admin, Permissions.MembersInvite, true)]
    [InlineData(CompanyRole.Admin, Permissions.BillingManage, false)]
    [InlineData(CompanyRole.Admin, Permissions.CompanyDelete, false)]
    [InlineData(CompanyRole.Reception, Permissions.AppointmentsRead, true)]
    [InlineData(CompanyRole.Reception, Permissions.MembersInvite, false)]
    [InlineData(CompanyRole.Professional, Permissions.AppointmentsSelfOnly, true)]
    [InlineData(CompanyRole.Professional, Permissions.MembersRead, false)]
    [InlineData(CompanyRole.SaaSAdministrator, Permissions.GlobalTenantManage, true)]
    [InlineData(CompanyRole.SaaSAdministrator, Permissions.CompanyDelete, true)]
    public void HasPermission_ValidatesRolePermissionsAccurately(CompanyRole role, string permission, bool expected)
    {
        var result = RolePermissionsMatrix.HasPermission(role, permission);
        result.Should().Be(expected);
    }
}
