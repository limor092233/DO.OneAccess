namespace DO.OneAccess.Application.Common.Interfaces;

public interface IBootstrapTokenService
{
    bool IsConfigured { get; }
    void Initialize(bool isSystemInitialized);
    bool ValidateToken(string? providedToken);
    void InvalidateToken();
}
