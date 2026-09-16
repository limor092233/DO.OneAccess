namespace DO.OneAccess.Domain.Entities;

public class RefreshToken
{
    public long RefreshTokenId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public long? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public RefreshToken? ReplacedByToken { get; set; }
    public ICollection<RefreshToken> ReplacedTokens { get; set; } = new List<RefreshToken>();
}
