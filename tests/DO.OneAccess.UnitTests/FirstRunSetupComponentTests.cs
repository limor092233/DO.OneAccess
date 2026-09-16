using System.Net;
using System.Reflection;
using DO.OneAccess.Application.DTOs.Setup;
using DO.OneAccess.Client.Auth;
using DO.OneAccess.Client.Pages;
using DO.OneAccess.Client.Services;
using DO.OneAccess.Client.Services.Api;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class FirstRunSetupComponentTests
{
    private class TestNavigationManager : NavigationManager
    {
        public string? NavigatedUri { get; private set; }

        public TestNavigationManager()
        {
            Initialize("http://localhost:5202/", "http://localhost:5202/setup");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedUri = uri;
        }
    }

    private class TestSetupStateService : ISetupStateService
    {
        public bool? IsInitialized { get; set; }
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
        public bool MarkedInitialized { get; private set; }

        public Task<bool> CheckInitializationAsync(CancellationToken ct = default) =>
            Task.FromResult(IsInitialized ?? false);

        public void MarkInitialized()
        {
            MarkedInitialized = true;
            IsInitialized = true;
        }
    }

    private class TestSetupApiClient : ISetupApiClient
    {
        public FirstRunSetupDto? CapturedDto { get; private set; }
        public string? CapturedToken { get; private set; }
        public Exception? ExceptionToThrow { get; set; }
        public FirstRunSetupResultDto ResultToReturn { get; set; } = new() { Success = true, Message = "Setup succeeded." };

        public Task<SetupStatusDto> GetStatusAsync(CancellationToken ct = default) =>
            Task.FromResult(new SetupStatusDto { IsInitialized = false });

        public Task<FirstRunSetupResultDto> SetupAsync(FirstRunSetupDto dto, string bootstrapToken, CancellationToken ct = default)
        {
            CapturedDto = dto;
            CapturedToken = bootstrapToken;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(ResultToReturn);
        }
    }

    private static (FirstRunSetup Component, TestSetupApiClient ApiClient, TestSetupStateService StateService, TestNavigationManager NavManager) CreateTestComponent()
    {
        var component = new FirstRunSetup();
        var apiClient = new TestSetupApiClient();
        var stateService = new TestSetupStateService();
        var navManager = new TestNavigationManager();

        InjectDependency(component, "SetupClient", apiClient);
        InjectDependency(component, "SetupState", stateService);
        InjectDependency(component, "Navigation", navManager);

        return (component, apiClient, stateService, navManager);
    }

    private static void InjectDependency(object target, string propertyName, object value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        prop?.SetValue(target, value);
    }

    private static void PopulateValidStep1(FirstRunSetup component)
    {
        component.BootstrapToken = "valid-bootstrap-token-123";
        component.FormModel.EmployeeNumber = "SA-0001";
        component.FormModel.Position = "Lead Systems Administrator";
        component.FormModel.FirstName = "Admin";
        component.FormModel.MiddleName = "System";
        component.FormModel.LastName = "Root";
        component.FormModel.Email = "sysadmin@organization.gov";
    }

    [Fact]
    public void Step1_InitialState_DefaultsToStepOne_AndFormInitialized()
    {
        var (component, _, _, _) = CreateTestComponent();

        Assert.Equal(1, component.CurrentStep);
        Assert.NotNull(component.FormModel);
        Assert.Empty(component.FieldErrors);
        Assert.Null(component.GeneralError);
        Assert.False(component.SetupCompleted);
    }

    [Fact]
    public void Step1_RequiredFields_PreventNext_WhenInvalid()
    {
        var (component, _, _, _) = CreateTestComponent();

        // Leaving all Step 1 fields blank
        component.BootstrapToken = "";
        component.FormModel.EmployeeNumber = "";
        component.FormModel.FirstName = "";
        component.FormModel.LastName = "";
        component.FormModel.Email = "";

        component.HandleNextStep();

        Assert.Equal(1, component.CurrentStep);
        Assert.True(component.FieldErrors.ContainsKey("BootstrapToken"));
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.EmployeeNumber)));
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.FirstName)));
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.LastName)));
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.Email)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@gov")]
    public void Step1_InvalidEmail_PreventsNext(string invalidEmail)
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.FormModel.Email = invalidEmail;

        component.HandleNextStep();

        Assert.Equal(1, component.CurrentStep);
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.Email)));
        Assert.Contains("valid format", component.FieldErrors[nameof(FirstRunSetupDto.Email)][0]);
    }

    [Fact]
    public void Step1_OptionalFields_DoNotPreventNext()
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.FormModel.Position = null;
        component.FormModel.MiddleName = null;

        component.HandleNextStep();

        Assert.Equal(2, component.CurrentStep);
        Assert.False(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.Position)));
        Assert.False(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.MiddleName)));
    }

    [Fact]
    public void Step1_ValidStep1_ProceedsToStep2()
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);

        component.HandleNextStep();

        Assert.Equal(2, component.CurrentStep);
        Assert.Empty(component.FieldErrors);
    }

    [Fact]
    public void Step2_Back_ReturnsToStep1_AndPreservesValues()
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();
        Assert.Equal(2, component.CurrentStep);

        // Enter step 2 credentials
        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "secret123";
        component.FormModel.ConfirmPassword = "secret123";

        // Click Back
        component.HandlePreviousStep();

        Assert.Equal(1, component.CurrentStep);
        Assert.Equal("valid-bootstrap-token-123", component.BootstrapToken);
        Assert.Equal("SA-0001", component.FormModel.EmployeeNumber);
        Assert.Equal("Lead Systems Administrator", component.FormModel.Position);
        Assert.Equal("Admin", component.FormModel.FirstName);
        Assert.Equal("System", component.FormModel.MiddleName);
        Assert.Equal("Root", component.FormModel.LastName);
        Assert.Equal("sysadmin@organization.gov", component.FormModel.Email);
        Assert.Equal("sysadmin", component.FormModel.Username);
        Assert.Equal("secret123", component.FormModel.Password);
        Assert.Equal("secret123", component.FormModel.ConfirmPassword);
    }

    [Fact]
    public void Step2_MissingUsername_FailsValidation()
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();

        component.FormModel.Username = "";
        component.FormModel.Password = "password123";
        component.FormModel.ConfirmPassword = "password123";

        var isValid = component.ValidateStep2();

        Assert.False(isValid);
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.Username)));
    }

    [Fact]
    public void Step2_MissingPassword_FailsValidation()
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();

        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "";
        component.FormModel.ConfirmPassword = "";

        var isValid = component.ValidateStep2();

        Assert.False(isValid);
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.Password)));
    }

    [Fact]
    public void Step2_MismatchedPasswords_FailsValidation()
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();

        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "password123";
        component.FormModel.ConfirmPassword = "different-password";

        var isValid = component.ValidateStep2();

        Assert.False(isValid);
        Assert.True(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.ConfirmPassword)));
        Assert.Contains("match", component.FieldErrors[nameof(FirstRunSetupDto.ConfirmPassword)][0]);
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("password")]
    [InlineData("admin")]
    public void Step2_Validation_DoesNotEnforceUnapprovedPasswordComplexity(string simplePassword)
    {
        var (component, _, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();

        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = simplePassword;
        component.FormModel.ConfirmPassword = simplePassword;

        var isValid = component.ValidateStep2();

        Assert.True(isValid);
        Assert.False(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.Password)));
        Assert.False(component.FieldErrors.ContainsKey(nameof(FirstRunSetupDto.ConfirmPassword)));
    }

    [Fact]
    public void FirstRunSetup_Markup_DoesNotContainEditableAssignedRoleField()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DO.OneAccess.sln")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);

        var razorFilePath = Path.Combine(dir.FullName, "src", "DO.OneAccess.Client", "Pages", "FirstRunSetup.razor");
        Assert.True(File.Exists(razorFilePath), $"FirstRunSetup.razor not found at {razorFilePath}");

        var content = File.ReadAllText(razorFilePath);

        // Assert no role input, select, or dropdown exists
        Assert.DoesNotContain("<select", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id=\"role\"", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id=\"assignedRole\"", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name=\"role\"", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"hidden\"", content, StringComparison.OrdinalIgnoreCase);

        // Assert static informational text exists
        Assert.Contains("System Administrator", content);
        Assert.Contains("Initial system account", content);
    }

    [Fact]
    public async Task Step2_CompleteSetup_SubmitsThroughSetupApiClient()
    {
        var (component, apiClient, stateService, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();

        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "SecurePass123";
        component.FormModel.ConfirmPassword = "SecurePass123";

        await component.HandleSetupSubmit();

        Assert.NotNull(apiClient.CapturedDto);
        Assert.Equal("sysadmin", apiClient.CapturedDto.Username);
        Assert.Equal("valid-bootstrap-token-123", apiClient.CapturedToken);
        Assert.True(component.SetupCompleted);
        Assert.True(stateService.MarkedInitialized);
    }

    [Fact]
    public async Task Step2_ErrorHandling_401_SetsGeneralError()
    {
        var (component, apiClient, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();
        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "pass123";
        component.FormModel.ConfirmPassword = "pass123";

        apiClient.ExceptionToThrow = new ProblemDetailsException(HttpStatusCode.Unauthorized, "Unauthorized", "Invalid bootstrap token");

        await component.HandleSetupSubmit();

        Assert.False(component.SetupCompleted);
        Assert.Contains("Invalid or expired bootstrap token", component.GeneralError);
    }

    [Fact]
    public async Task Step2_ErrorHandling_409_NavigatesToLoginAlreadyInitialized()
    {
        var (component, apiClient, stateService, navManager) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();
        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "pass123";
        component.FormModel.ConfirmPassword = "pass123";

        apiClient.ExceptionToThrow = new ProblemDetailsException(HttpStatusCode.Conflict, "Conflict", "Already initialized");

        await component.HandleSetupSubmit();

        Assert.True(stateService.MarkedInitialized);
        Assert.Equal("login?alreadyInitialized=true", navManager.NavigatedUri);
    }

    [Fact]
    public async Task Step2_ErrorHandling_429_SetsRateLimitError()
    {
        var (component, apiClient, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();
        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "pass123";
        component.FormModel.ConfirmPassword = "pass123";

        apiClient.ExceptionToThrow = new ProblemDetailsException((HttpStatusCode)429, "Too Many Requests", "Too many setup attempts.");

        await component.HandleSetupSubmit();

        Assert.False(component.SetupCompleted);
        Assert.Contains("Too many setup attempts", component.GeneralError);
    }

    [Fact]
    public async Task Step2_ErrorHandling_503_SetsUnavailableError()
    {
        var (component, apiClient, _, _) = CreateTestComponent();
        PopulateValidStep1(component);
        component.HandleNextStep();
        component.FormModel.Username = "sysadmin";
        component.FormModel.Password = "pass123";
        component.FormModel.ConfirmPassword = "pass123";

        apiClient.ExceptionToThrow = new ProblemDetailsException(HttpStatusCode.ServiceUnavailable, "Service Unavailable");

        await component.HandleSetupSubmit();

        Assert.False(component.SetupCompleted);
        Assert.Contains("Setup is temporarily unavailable", component.GeneralError);
    }
}
