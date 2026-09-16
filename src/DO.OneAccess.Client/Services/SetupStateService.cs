using DO.OneAccess.Client.Services.Api;

namespace DO.OneAccess.Client.Services;

public class SetupStateService : ISetupStateService
{
    private readonly ISetupApiClient _setupApiClient;
    private bool? _isInitialized;
    private bool _hasError;
    private string? _errorMessage;

    public bool? IsInitialized => _isInitialized;
    public bool HasError => _hasError;
    public string? ErrorMessage => _errorMessage;

    public SetupStateService(ISetupApiClient setupApiClient)
    {
        _setupApiClient = setupApiClient;
    }

    public async Task<bool> CheckInitializationAsync(CancellationToken ct = default)
    {
        if (_isInitialized.HasValue)
        {
            return _isInitialized.Value;
        }

        try
        {
            var status = await _setupApiClient.GetStatusAsync(ct);
            _isInitialized = status.IsInitialized;
            _hasError = false;
            _errorMessage = null;
            return _isInitialized.Value;
        }
        catch
        {
            _hasError = true;
            _errorMessage = "Unable to connect to the DO.OneAccess server to verify setup status. Please verify your connection.";
            return false;
        }
    }

    public void MarkInitialized()
    {
        _isInitialized = true;
        _hasError = false;
        _errorMessage = null;
    }
}
