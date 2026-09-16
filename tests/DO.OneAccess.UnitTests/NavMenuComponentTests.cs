using System.IO;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class NavMenuComponentTests
{
    private static string GetClientFilePath(string relativePath)
    {
        // Finds the client project directory from tests execution path
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DO.OneAccess.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException("Could not find DO.OneAccess solution directory");
        }

        return Path.Combine(dir.FullName, "src", "DO.OneAccess.Client", relativePath);
    }

    [Fact]
    public void NavMenu_Razor_ContainsExpectedStructureAndIcons()
    {
        var navMenuPath = GetClientFilePath(Path.Combine("Layout", "NavMenu.razor"));
        Assert.True(File.Exists(navMenuPath), "NavMenu.razor must exist");

        var content = File.ReadAllText(navMenuPath);

        // Sidebar nav container
        Assert.Contains("class=\"sidebar-nav\"", content);

        // Section titles
        Assert.Contains("<div class=\"sidebar-section-title\">PORTAL</div>", content);
        Assert.Contains("<div class=\"sidebar-section-title\">ADMINISTRATION</div>", content);
        Assert.Contains("<div class=\"sidebar-section-title\">SYSTEM ADMINISTRATION</div>", content);

        // Links with .sidebar-link, .nav-icon, and .nav-label
        Assert.Contains("class=\"sidebar-link\"", content);
        Assert.Contains("class=\"nav-icon\"", content);
        Assert.Contains("class=\"nav-label\"", content);

        // Required navigation icons
        Assert.Contains("<Icon Name=\"grid\" Size=\"18\" />", content);
        Assert.Contains("<Icon Name=\"users\" Size=\"18\" />", content);
        Assert.Contains("<Icon Name=\"building\" Size=\"18\" />", content);
        Assert.Contains("<Icon Name=\"key\" Size=\"18\" />", content);
        Assert.Contains("<Icon Name=\"shield\" Size=\"18\" />", content);
        Assert.Contains("<Icon Name=\"list\" Size=\"18\" />", content);
        Assert.Contains("<Icon Name=\"clock\" Size=\"18\" />", content);

        // Labels
        Assert.Contains("Registered Systems", content);
        Assert.Contains("Manage Users", content);
        Assert.Contains("Manage Division", content);
        Assert.Contains("System Access", content);
        Assert.Contains("Admin Scopes", content);
        Assert.Contains("Audit Logs", content);
        Assert.Contains("Login History", content);

        // Confirm branding/logo is NOT in NavMenu markup
        var markup = string.Join("\n", File.ReadAllLines(navMenuPath).Where(l => !l.TrimStart().StartsWith("@namespace")));
        Assert.DoesNotContain("DO.OneAccess", markup);
        Assert.DoesNotContain("<img", markup);
    }

    [Fact]
    public void NavMenu_Css_ContainsRequiredStyles()
    {
        var cssPath = GetClientFilePath(Path.Combine("Layout", "NavMenu.razor.css"));
        Assert.True(File.Exists(cssPath), "NavMenu.razor.css must exist");

        var css = File.ReadAllText(cssPath);

        // .sidebar-link styles
        Assert.Contains(".sidebar-link", css);
        Assert.Contains("gap: 10px", css);
        Assert.Contains("min-height: 42px", css);
        Assert.Contains("padding: 10px 20px", css);
        Assert.Contains("display: flex", css);

        // .nav-icon styles
        Assert.Contains(".nav-icon", css);
        Assert.Contains("flex: 0 0 22px", css);
        Assert.Contains("width: 22px", css);
        Assert.Contains("height: 22px", css);
        Assert.Contains("display: inline-flex", css);

        // .nav-label styles
        Assert.Contains(".nav-label", css);

        // Active and hover states
        Assert.Contains(".sidebar-link:hover", css);
        Assert.Contains(".sidebar-link.active", css);
    }

    [Fact]
    public void MainLayout_Css_SidebarHasRightDivider()
    {
        var cssPath = GetClientFilePath(Path.Combine("Layout", "MainLayout.razor.css"));
        Assert.True(File.Exists(cssPath), "MainLayout.razor.css must exist");

        var css = File.ReadAllText(cssPath);
        Assert.Contains(".app-sidebar", css);
        Assert.Contains("border-right: 1px solid #dfe5ec;", css);
        Assert.Contains("box-shadow: none;", css);
    }

    [Fact]
    public void MainLayout_Css_SidebarAndMainLayoutHaveSeamlessBackgroundColor()
    {
        var cssPath = GetClientFilePath(Path.Combine("Layout", "MainLayout.razor.css"));
        Assert.True(File.Exists(cssPath), "MainLayout.razor.css must exist");

        var css = File.ReadAllText(cssPath);
        Assert.Contains(".app-sidebar", css);
        Assert.Contains(".app-main", css);
        // Sidebar and main content share #ffffff background for seamless appearance
        Assert.Contains("background-color: #ffffff;", css);
    }
}
