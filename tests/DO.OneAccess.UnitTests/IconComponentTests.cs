#pragma warning disable BL0005
using DO.OneAccess.Client.Components.Common;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class IconComponentTests
{
    [Fact]
    public void Icon_DefaultValues_AreExpected()
    {
        var icon = new Icon();

        Assert.Equal(string.Empty, icon.Name);
        Assert.Equal(18, icon.Size);
        Assert.Null(icon.CssClass);
    }

    [Theory]
    [InlineData("grid")]
    [InlineData("users")]
    [InlineData("badge")]
    [InlineData("folder")]
    [InlineData("key")]
    [InlineData("building")]
    [InlineData("server")]
    [InlineData("shield")]
    [InlineData("list")]
    [InlineData("clock")]
    [InlineData("logout")]
    [InlineData("check")]
    [InlineData("x")]
    [InlineData("alert-triangle")]
    [InlineData("lock")]
    [InlineData("external-link")]
    [InlineData("search")]
    [InlineData("plus")]
    [InlineData("trash")]
    [InlineData("edit")]
    [InlineData("refresh")]
    public void Icon_KnownNames_CanBeInstantiatedWithoutError(string iconName)
    {
        var icon = new Icon
        {
            Name = iconName,
            Size = 24,
            CssClass = "test-icon"
        };

        Assert.Equal(iconName, icon.Name);
        Assert.Equal(24, icon.Size);
        Assert.Equal("test-icon", icon.CssClass);
    }

    [Fact]
    public void Icon_UnknownName_DoesNotThrow()
    {
        var icon = new Icon
        {
            Name = "completely-unknown-icon-name-xyz",
            Size = 16
        };

        Assert.Equal("completely-unknown-icon-name-xyz", icon.Name);
        Assert.Equal(16, icon.Size);
    }
}
