using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DO.OneAccess.Infrastructure.Security;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class BootstrapTokenServiceTests
{
    [Fact]
    public void Initialize_Development_GeneratesSecureHexTokenWhenNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();

        var service = new BootstrapTokenService(config, NullLogger<BootstrapTokenService>.Instance);
        service.Initialize(isSystemInitialized: false);

        Assert.True(service.IsConfigured);
        Assert.False(service.ValidateToken("invalid-token"));
    }

    [Fact]
    public void Initialize_Development_UsesConfiguredToken()
    {
        const string expectedToken = "dev-secret-token-12345";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ONEACCESS_BOOTSTRAP_TOKEN"] = expectedToken
            })
            .Build();

        var service = new BootstrapTokenService(config, NullLogger<BootstrapTokenService>.Instance);
        service.Initialize(isSystemInitialized: false);

        Assert.True(service.IsConfigured);
        Assert.True(service.ValidateToken(expectedToken));
        Assert.False(service.ValidateToken("wrong-token"));
    }

    [Fact]
    public void Initialize_Production_FailsClosedWhenTokenNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        var service = new BootstrapTokenService(config, NullLogger<BootstrapTokenService>.Instance);
        service.Initialize(isSystemInitialized: false);

        Assert.False(service.IsConfigured);
        Assert.False(service.ValidateToken("any-token"));
    }

    [Fact]
    public void Initialize_Production_UsesConfiguredToken()
    {
        const string expectedToken = "prod-secret-token-99999";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["ONEACCESS_BOOTSTRAP_TOKEN"] = expectedToken
            })
            .Build();

        var service = new BootstrapTokenService(config, NullLogger<BootstrapTokenService>.Instance);
        service.Initialize(isSystemInitialized: false);

        Assert.True(service.IsConfigured);
        Assert.True(service.ValidateToken(expectedToken));
        Assert.False(service.ValidateToken("wrong-token"));
    }

    [Fact]
    public void InvalidateToken_PermanentlyDisablesValidation()
    {
        const string expectedToken = "test-token-to-invalidate";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ONEACCESS_BOOTSTRAP_TOKEN"] = expectedToken
            })
            .Build();

        var service = new BootstrapTokenService(config, NullLogger<BootstrapTokenService>.Instance);
        service.Initialize(isSystemInitialized: false);

        Assert.True(service.ValidateToken(expectedToken));

        service.InvalidateToken();

        Assert.False(service.IsConfigured);
        Assert.False(service.ValidateToken(expectedToken));
    }

    [Fact]
    public void Initialize_WhenSystemAlreadyInitialized_DoesNotConfigureToken()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ONEACCESS_BOOTSTRAP_TOKEN"] = "token"
            })
            .Build();

        var service = new BootstrapTokenService(config, NullLogger<BootstrapTokenService>.Instance);
        service.Initialize(isSystemInitialized: true);

        Assert.False(service.IsConfigured);
        Assert.False(service.ValidateToken("token"));
    }
}
