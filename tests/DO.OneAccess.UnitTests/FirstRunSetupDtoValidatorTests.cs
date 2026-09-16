using DO.OneAccess.Application.Common.Exceptions;
using DO.OneAccess.Application.DTOs.Setup;
using DO.OneAccess.Application.Validators;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class FirstRunSetupDtoValidatorTests
{
    private static FirstRunSetupDto CreateValidDto() => new()
    {
        EmployeeNumber = "SA-0001",
        FirstName = "Admin",
        MiddleName = "System",
        LastName = "Root",
        Email = "sysadmin@organization.gov",
        Position = "Lead Architect",
        Username = "sysadmin",
        Password = "SecurePassword123!",
        ConfirmPassword = "SecurePassword123!"
    };

    [Fact]
    public void Validate_ValidDto_DoesNotThrow()
    {
        var dto = CreateValidDto();
        var ex = Record.Exception(() => FirstRunSetupDtoValidator.Validate(dto));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEmployeeNumber_ThrowsValidationException(string employeeNumber)
    {
        var dto = CreateValidDto();
        dto.EmployeeNumber = employeeNumber;

        var ex = Assert.Throws<ValidationException>(() => FirstRunSetupDtoValidator.Validate(dto));
        Assert.True(ex.Errors.ContainsKey(nameof(dto.EmployeeNumber)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingFirstName_ThrowsValidationException(string firstName)
    {
        var dto = CreateValidDto();
        dto.FirstName = firstName;

        var ex = Assert.Throws<ValidationException>(() => FirstRunSetupDtoValidator.Validate(dto));
        Assert.True(ex.Errors.ContainsKey(nameof(dto.FirstName)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingLastName_ThrowsValidationException(string lastName)
    {
        var dto = CreateValidDto();
        dto.LastName = lastName;

        var ex = Assert.Throws<ValidationException>(() => FirstRunSetupDtoValidator.Validate(dto));
        Assert.True(ex.Errors.ContainsKey(nameof(dto.LastName)));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("admin@")]
    [InlineData("@org.gov")]
    public void Validate_InvalidEmail_ThrowsValidationException(string email)
    {
        var dto = CreateValidDto();
        dto.Email = email;

        var ex = Assert.Throws<ValidationException>(() => FirstRunSetupDtoValidator.Validate(dto));
        Assert.True(ex.Errors.ContainsKey(nameof(dto.Email)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingPassword_ThrowsValidationException(string password)
    {
        var dto = CreateValidDto();
        dto.Password = password;
        dto.ConfirmPassword = password;

        var ex = Assert.Throws<ValidationException>(() => FirstRunSetupDtoValidator.Validate(dto));
        Assert.True(ex.Errors.ContainsKey(nameof(dto.Password)));
    }

    [Fact]
    public void Validate_MismatchedConfirmPassword_ThrowsValidationException()
    {
        var dto = CreateValidDto();
        dto.ConfirmPassword = "DifferentPassword123!";

        var ex = Assert.Throws<ValidationException>(() => FirstRunSetupDtoValidator.Validate(dto));
        Assert.True(ex.Errors.ContainsKey(nameof(dto.ConfirmPassword)));
    }
}
