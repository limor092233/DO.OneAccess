using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DO.OneAccess.Application.Common.Interfaces;

namespace DO.OneAccess.Infrastructure.Security;

public class BootstrapTokenService : IBootstrapTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BootstrapTokenService> _logger;
    private readonly object _lock = new();

    private string? _activeToken;
    private bool _isConfigured;

    public bool IsConfigured
    {
        get
        {
            lock (_lock)
            {
                return _isConfigured;
            }
        }
    }

    public BootstrapTokenService(
        IConfiguration configuration,
        ILogger<BootstrapTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Initialize(bool isSystemInitialized)
    {
        lock (_lock)
        {
            if (isSystemInitialized)
            {
                _activeToken = null;
                _isConfigured = false;
                return;
            }

            var env = _configuration["ASPNETCORE_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Production";

            var isDevelopment = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
            var envToken = _configuration["ONEACCESS_BOOTSTRAP_TOKEN"]
                ?? Environment.GetEnvironmentVariable("ONEACCESS_BOOTSTRAP_TOKEN");

            if (isDevelopment)
            {
                if (!string.IsNullOrWhiteSpace(envToken))
                {
                    _activeToken = envToken.Trim();
                    _isConfigured = true;
                }
                else
                {
                    _activeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
                    _isConfigured = true;
                }

                Console.WriteLine();
                Console.WriteLine("================================================================================");
                Console.WriteLine("[FIRST RUN SETUP] DO.OneAccess requires initial System Administrator setup.");
                Console.WriteLine($"Bootstrap Token: {_activeToken}");
                Console.WriteLine("Supply this token via the 'X-Bootstrap-Token' HTTP header during setup.");
                Console.WriteLine("================================================================================");
                Console.WriteLine();

                _logger.LogInformation("Development bootstrap token initialized and displayed to console.");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(envToken))
                {
                    _activeToken = envToken.Trim();
                    _isConfigured = true;
                    _logger.LogInformation("Production bootstrap token loaded from configuration.");
                }
                else
                {
                    _activeToken = null;
                    _isConfigured = false;
                    _logger.LogWarning(
                        "First Run Setup is required, but ONEACCESS_BOOTSTRAP_TOKEN is not configured. " +
                        "Setup endpoints are disabled (fail-closed) until configuration is provided.");
                }
            }
        }
    }

    public bool ValidateToken(string? providedToken)
    {
        lock (_lock)
        {
            if (!_isConfigured || string.IsNullOrEmpty(_activeToken) || string.IsNullOrWhiteSpace(providedToken))
            {
                return false;
            }

            var activeBytes = Encoding.UTF8.GetBytes(_activeToken);
            var providedBytes = Encoding.UTF8.GetBytes(providedToken.Trim());

            if (activeBytes.Length != providedBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(activeBytes, providedBytes);
        }
    }

    public void InvalidateToken()
    {
        lock (_lock)
        {
            _activeToken = null;
            _isConfigured = false;
            _logger.LogInformation("Bootstrap token invalidated.");
        }
    }
}
