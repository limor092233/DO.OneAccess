using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher;

    public PasswordHasher(IOptions<PasswordHasherOptions>? options = null)
    {
        _hasher = new PasswordHasher<User>(options);
    }

    public string HashPassword(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public PasswordVerifyResult VerifyPassword(User user, string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(password))
        {
            return new PasswordVerifyResult(false, false);
        }

        try
        {
            var result = _hasher.VerifyHashedPassword(user, passwordHash, password);
            return result switch
            {
                PasswordVerificationResult.Success => new PasswordVerifyResult(true, false),
                PasswordVerificationResult.SuccessRehashNeeded => new PasswordVerifyResult(true, true),
                _ => new PasswordVerifyResult(false, false)
            };
        }
        catch
        {
            return new PasswordVerifyResult(false, false);
        }
    }
}
