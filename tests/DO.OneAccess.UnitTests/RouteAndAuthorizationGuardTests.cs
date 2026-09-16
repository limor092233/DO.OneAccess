using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using DO.OneAccess.Client.Pages;
using DO.OneAccess.Client.Pages.Admin;
using DO.OneAccess.Client.Pages.SystemAdmin;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class RouteAndAuthorizationGuardTests
{
    [Theory]
    [InlineData(typeof(Dashboard), "/systems", null)]
    [InlineData(typeof(Dashboard), "/dashboard", null)]
    [InlineData(typeof(ManageSections), "/admin/sections", "ADMINISTRATOR,SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(ManageUsers), "/admin/employees", "ADMINISTRATOR,SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(ManageUsers), "/admin/users", "ADMINISTRATOR,SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(ManageSystemAccess), "/admin/system-access", "ADMINISTRATOR,SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(ManageDivisions), "/sysadmin/divisions", "SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(ManageSystems), "/sysadmin/systems", "SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(ManageAdminScopes), "/sysadmin/scopes", "SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(AuditLogs), "/sysadmin/audit-logs", "SYSTEM_ADMINISTRATOR")]
    [InlineData(typeof(LoginHistories), "/sysadmin/login-histories", "SYSTEM_ADMINISTRATOR")]
    public void Page_ShouldHaveCorrectRouteAndAuthorizeAttributes(Type pageType, string expectedRoute, string? expectedRoles)
    {
        // 1. Verify RouteAttribute
        var routeTemplates = pageType.GetCustomAttributes<RouteAttribute>().Select(r => r.Template).ToList();
        Assert.NotEmpty(routeTemplates);
        Assert.Contains(expectedRoute, routeTemplates);

        // 2. Verify AuthorizeAttribute
        var authorizeAttr = pageType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);

        if (expectedRoles != null)
        {
            Assert.Equal(expectedRoles, authorizeAttr.Roles);
        }
        else
        {
            Assert.Null(authorizeAttr.Roles); // Any authenticated user
        }
    }

    [Theory]
    [InlineData(typeof(Login), "/login")]
    [InlineData(typeof(AccessDenied), "/access-denied")]
    public void PublicPages_ShouldNotRequireAuthorize(Type pageType, string expectedRoute)
    {
        var routeAttr = pageType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttr);
        Assert.Equal(expectedRoute, routeAttr.Template);

        var authorizeAttr = pageType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.Null(authorizeAttr);
    }
}
