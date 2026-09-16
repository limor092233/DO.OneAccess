using System.Text.RegularExpressions;
using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Setup;

namespace DO.OneAccess.Application.Validators;

public static class FirstRunSetupDtoValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Validate(FirstRunSetupDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.EmployeeNumber))
        {
            errors[nameof(dto.EmployeeNumber)] = new[] { "Employee Number is required." };
        }
        else if (dto.EmployeeNumber.Length > 50)
        {
            errors[nameof(dto.EmployeeNumber)] = new[] { "Employee Number must not exceed 50 characters." };
        }

        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            errors[nameof(dto.FirstName)] = new[] { "First Name is required." };
        }
        else if (dto.FirstName.Length > 100)
        {
            errors[nameof(dto.FirstName)] = new[] { "First Name must not exceed 100 characters." };
        }

        if (dto.MiddleName != null && dto.MiddleName.Length > 100)
        {
            errors[nameof(dto.MiddleName)] = new[] { "Middle Name must not exceed 100 characters." };
        }

        if (string.IsNullOrWhiteSpace(dto.LastName))
        {
            errors[nameof(dto.LastName)] = new[] { "Last Name is required." };
        }
        else if (dto.LastName.Length > 100)
        {
            errors[nameof(dto.LastName)] = new[] { "Last Name must not exceed 100 characters." };
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            errors[nameof(dto.Email)] = new[] { "Email address is required." };
        }
        else if (dto.Email.Length > 200)
        {
            errors[nameof(dto.Email)] = new[] { "Email address must not exceed 200 characters." };
        }
        else if (!EmailRegex.IsMatch(dto.Email))
        {
            errors[nameof(dto.Email)] = new[] { "Email address is not in a valid format." };
        }

        if (dto.Position != null && dto.Position.Length > 100)
        {
            errors[nameof(dto.Position)] = new[] { "Position must not exceed 100 characters." };
        }

        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            errors[nameof(dto.Username)] = new[] { "Username is required." };
        }
        else if (dto.Username.Length > 50)
        {
            errors[nameof(dto.Username)] = new[] { "Username must not exceed 50 characters." };
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            errors[nameof(dto.Password)] = new[] { "Password is required." };
        }

        if (string.IsNullOrWhiteSpace(dto.ConfirmPassword))
        {
            errors[nameof(dto.ConfirmPassword)] = new[] { "Password confirmation is required." };
        }
        else if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
        {
            errors[nameof(dto.ConfirmPassword)] = new[] { "Passwords do not match." };
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
