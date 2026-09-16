namespace DO.OneAccess.Client.Services;

public interface ISetupStateService
{
    bool? IsInitialized { get; }
    bool HasError { get; }
    string? ErrorMessage { get; }
    Task<bool> CheckInitializationAsync(CancellationToken ct = default);
    void MarkInitialized();
}
