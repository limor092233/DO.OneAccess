using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.Common.Interfaces;
using DO.OneAccess.Application.DTOs.Setup;
using DO.OneAccess.Application.Validators;
using DO.OneAccess.Domain.Entities;
using DO.OneAccess.Domain.Enums;

namespace DO.OneAccess.Application.Services;

public class FirstRunSetupService : IFirstRunSetupService
{
    private static readonly SemaphoreSlim _setupLock = new(1, 1);
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBootstrapTokenService _bootstrapTokenService;

    public FirstRunSetupService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IBootstrapTokenService bootstrapTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _bootstrapTokenService = bootstrapTokenService;
    }

    public async Task<SetupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var isInitialized = await _context.Users
            .AnyAsync(u => u.RoleId == (short)RoleType.SystemAdministrator, cancellationToken);

        return new SetupStatusDto
        {
            IsInitialized = isInitialized
        };
    }

    public async Task<FirstRunSetupResultDto> SetupAsync(
        FirstRunSetupDto dto,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        FirstRunSetupDtoValidator.Validate(dto);

        await _setupLock.WaitAsync(cancellationToken);
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            var isInitialized = await _context.Users
                .AnyAsync(u => u.RoleId == (short)RoleType.SystemAdministrator, cancellationToken);

            if (isInitialized)
            {
                throw new ConflictException("System is already initialized.");
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == dto.Username.Trim(), cancellationToken);

            if (usernameExists)
            {
                throw new ConflictException($"Username '{dto.Username.Trim()}' is already in use.");
            }

            var employeeNumberExists = await _context.Users
                .AnyAsync(u => u.EmployeeNumber == dto.EmployeeNumber.Trim(), cancellationToken);

            if (employeeNumberExists)
            {
                throw new ConflictException($"Employee number '{dto.EmployeeNumber.Trim()}' is already in use.");
            }

            var now = DateTime.UtcNow;
            var userId = Guid.NewGuid();

            var user = new User
            {
                UserId = userId,
                EmployeeNumber = dto.EmployeeNumber.Trim(),
                FirstName = dto.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position.Trim(),
                SectionId = null,
                RoleId = (short)RoleType.SystemAdministrator,
                Username = dto.Username.Trim(),
                IsActive = true,
                CreatedAt = now,
                CreatedBy = null
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            _context.Users.Add(user);

            var auditPayload = new
            {
                Username = user.Username,
                RoleId = user.RoleId,
                EmployeeNumber = user.EmployeeNumber,
                Email = user.Email,
                UserAgent = userAgent
            };

            var auditLog = new AuditLog
            {
                UserId = null,
                Action = "SYSTEM_BOOTSTRAP",
                EntityName = nameof(User),
                EntityId = userId.ToString(),
                IpAddress = clientIp,
                CreatedAt = now,
                NewValues = JsonSerializer.Serialize(auditPayload)
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _bootstrapTokenService.InvalidateToken();

            return new FirstRunSetupResultDto
            {
                Success = true,
                Message = "System Administrator initialized successfully. Proceed to sign in.",
                Username = user.Username
            };
        }
        finally
        {
            _setupLock.Release();
        }
    }
}
