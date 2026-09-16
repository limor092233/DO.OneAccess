using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Infrastructure.Security;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class PasswordHasherTests
{
    private readonly User _testUser;

    public PasswordHasherTests()
    {
        _testUser = new User
        {
            UserId = Guid.NewGuid(),
            Username = "testuser"
        };
    }

    [Fact]
    public void HashPassword_ShouldProduceNonEmptyHash()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword(_testUser, "SecureP@ssw0rd123!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("SecureP@ssw0rd123!", hash);
    }

    [Fact]
    public void HashPassword_ShouldProduceDifferentHashesForSamePassword_DueToSalt()
    {
        var hasher = new PasswordHasher();
        var hash1 = hasher.HashPassword(_testUser, "SecureP@ssw0rd123!");
        var hash2 = hasher.HashPassword(_testUser, "SecureP@ssw0rd123!");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnVerified()
    {
        var hasher = new PasswordHasher();
        var password = "CorrectPassword#2026";
        var hash = hasher.HashPassword(_testUser, password);

        var result = hasher.VerifyPassword(_testUser, password, hash);

        Assert.True(result.Verified);
        Assert.False(result.RehashNeeded);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnNotVerified()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword(_testUser, "CorrectPassword#2026");

        var result = hasher.VerifyPassword(_testUser, "WrongPassword#999", hash);

        Assert.False(result.Verified);
        Assert.False(result.RehashNeeded);
    }

    [Fact]
    public void VerifyPassword_WhenWorkFactorUpgraded_ShouldFlagRehashNeeded()
    {
        // Hash with 50,000 iterations
        var oldOptions = Options.Create(new PasswordHasherOptions { IterationCount = 50000 });
        var oldHasher = new PasswordHasher(oldOptions);
        var password = "UpgradeMyWorkFactor!";
        var hash = oldHasher.HashPassword(_testUser, password);

        // Verify with 100,000 iterations
        var newOptions = Options.Create(new PasswordHasherOptions { IterationCount = 100000 });
        var newHasher = new PasswordHasher(newOptions);
        var result = newHasher.VerifyPassword(_testUser, password, hash);

        Assert.True(result.Verified);
        Assert.True(result.RehashNeeded);
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("password", "")]
    [InlineData("password", "invalid_base64_hash")]
    public void VerifyPassword_WithMalformedInputs_ShouldReturnNotVerified(string password, string hash)
    {
        var hasher = new PasswordHasher();
        var result = hasher.VerifyPassword(_testUser, password, hash);

        Assert.False(result.Verified);
    }
}
