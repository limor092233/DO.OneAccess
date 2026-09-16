using DO.OneAccess.Domain.Entities;

namespace DO.OneAccess.Application.Common.Interfaces;

public record PasswordVerifyResult(bool Verified, bool RehashNeeded);

public interface IPasswordHasher
{
    string HashPassword(User user, string password);
    PasswordVerifyResult VerifyPassword(User user, string password, string passwordHash);
}
