#pragma warning disable BL0005
using System.Reflection;
using DO.OneAccess.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace DO.OneAccess.UnitTests;

public class ClientComponentBehaviorTests
{
    private static object? GetNonPublicProperty(object instance, string propertyName)
    {
        var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return prop?.GetValue(instance);
    }

    private static async Task InvokeNonPublicMethodAsync(object instance, string methodName, params object?[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null) throw new MissingMethodException(instance.GetType().FullName, methodName);
        var result = method.Invoke(instance, parameters);
        if (result is Task task)
        {
            await task;
        }
    }

    [Fact]
    public void StatusBadge_ActiveAndInactive_RendersExpectedTextAndClass()
    {
        var activeBadge = new StatusBadge { IsActive = true };
        Assert.Equal("Active", GetNonPublicProperty(activeBadge, "Text"));
        var activeClass = GetNonPublicProperty(activeBadge, "BadgeClass") as string;
        Assert.Contains("bg-success-subtle", activeClass);

        var inactiveBadge = new StatusBadge { IsActive = false };
        Assert.Equal("Inactive", GetNonPublicProperty(inactiveBadge, "Text"));
        var inactiveClass = GetNonPublicProperty(inactiveBadge, "BadgeClass") as string;
        Assert.Contains("bg-secondary-subtle", inactiveClass);
    }

    [Theory]
    [InlineData("SYSTEM_ADMINISTRATOR", "System Admin", "bg-primary-subtle")]
    [InlineData("ADMINISTRATOR", "Administrator", "bg-info-subtle")]
    [InlineData("USER", "User", "bg-light")]
    public void StatusBadge_Roles_RendersExpectedLabelAndClass(string roleCode, string expectedLabel, string expectedClassSub)
    {
        var badge = new StatusBadge { RoleCode = roleCode };
        Assert.Equal(expectedLabel, GetNonPublicProperty(badge, "Text"));
        var badgeClass = GetNonPublicProperty(badge, "BadgeClass") as string;
        Assert.Contains(expectedClassSub, badgeClass);
    }

    [Theory]
    [InlineData("Allow", "bg-success-subtle")]
    [InlineData("Deny", "bg-danger-subtle")]
    public void StatusBadge_AccessType_RendersExpectedBadgeClass(string accessType, string expectedClassSub)
    {
        var badge = new StatusBadge { AccessType = accessType };
        Assert.Equal(accessType, GetNonPublicProperty(badge, "Text"));
        var badgeClass = GetNonPublicProperty(badge, "BadgeClass") as string;
        Assert.Contains(expectedClassSub, badgeClass);
    }

    [Fact]
    public void StatusBadge_CustomOverrides_TakePrecedence()
    {
        var badge = new StatusBadge
        {
            IsActive = true,
            CustomText = "Special State",
            CustomClass = "my-custom-badge"
        };

        Assert.Equal("Special State", GetNonPublicProperty(badge, "Text"));
        Assert.Equal("my-custom-badge", GetNonPublicProperty(badge, "BadgeClass"));
    }

    [Fact]
    public void Pagination_DefaultState_AndPageCalculations()
    {
        var pagination = new Pagination
        {
            CurrentPage = 5,
            TotalPages = 10,
            TotalCount = 100,
            HasPreviousPage = true,
            HasNextPage = true
        };

        Assert.Equal(5, pagination.CurrentPage);
        Assert.Equal(10, pagination.TotalPages);
        Assert.Equal(100, pagination.TotalCount);
        Assert.True(pagination.HasPreviousPage);
        Assert.True(pagination.HasNextPage);

        var startPage = (int)GetNonPublicProperty(pagination, "StartPage")!;
        var endPage = (int)GetNonPublicProperty(pagination, "EndPage")!;
        Assert.Equal(3, startPage); // 5 - 2
        Assert.Equal(7, endPage);   // 5 + 2
    }

    [Fact]
    public async Task Pagination_GoToPage_InvokesCallbackOnlyWhenValidAndChanged()
    {
        var invokedPage = 0;
        var pagination = new Pagination
        {
            CurrentPage = 3,
            TotalPages = 5,
            OnPageChanged = EventCallback.Factory.Create<int>(this, (p) => invokedPage = p)
        };

        // Same page -> do not invoke
        await InvokeNonPublicMethodAsync(pagination, "GoToPage", 3);
        Assert.Equal(0, invokedPage);

        // Out of range (< 1 or > TotalPages) -> do not invoke
        await InvokeNonPublicMethodAsync(pagination, "GoToPage", 0);
        Assert.Equal(0, invokedPage);

        await InvokeNonPublicMethodAsync(pagination, "GoToPage", 6);
        Assert.Equal(0, invokedPage);

        // Valid new page -> invoke callback
        await InvokeNonPublicMethodAsync(pagination, "GoToPage", 4);
        Assert.Equal(4, invokedPage);
    }

    [Fact]
    public async Task ConfirmDialog_ConfirmAndCancel_InvokesRespectiveCallbacks()
    {
        var confirmed = false;
        var cancelled = false;

        var dialog = new ConfirmDialog
        {
            IsOpen = true,
            Title = "Delete Employee",
            Message = "Are you sure?",
            ConfirmText = "Yes, Delete",
            CancelText = "Nevermind",
            ConfirmButtonClass = "btn-danger",
            IsBusy = false,
            OnConfirm = EventCallback.Factory.Create(this, () => confirmed = true),
            OnCancel = EventCallback.Factory.Create(this, () => cancelled = true)
        };

        Assert.True(dialog.IsOpen);
        Assert.Equal("Delete Employee", dialog.Title);
        Assert.Equal("Are you sure?", dialog.Message);
        Assert.Equal("Yes, Delete", dialog.ConfirmText);
        Assert.Equal("Nevermind", dialog.CancelText);
        Assert.Equal("btn-danger", dialog.ConfirmButtonClass);
        Assert.False(dialog.IsBusy);

        await InvokeNonPublicMethodAsync(dialog, "Confirm");
        Assert.True(confirmed);
        Assert.False(cancelled);

        await InvokeNonPublicMethodAsync(dialog, "Cancel");
        Assert.True(cancelled);
    }

    [Fact]
    public void LoadingSpinner_DefaultAndCustomParameters()
    {
        var defaultSpinner = new LoadingSpinner();
        Assert.Equal("Loading...", defaultSpinner.Text);
        Assert.True(defaultSpinner.Center);
        Assert.False(defaultSpinner.Small);

        var centerClass = (string)GetNonPublicProperty(defaultSpinner, "ContainerClass")!;
        Assert.Contains("justify-content-center", centerClass);

        var defaultSpinnerClass = (string)GetNonPublicProperty(defaultSpinner, "SpinnerClass")!;
        Assert.DoesNotContain("spinner-border-sm", defaultSpinnerClass);

        var smallInlineSpinner = new LoadingSpinner
        {
            Center = false,
            Small = true,
            Text = "Please wait..."
        };

        var inlineClass = (string)GetNonPublicProperty(smallInlineSpinner, "ContainerClass")!;
        Assert.Contains("d-inline-flex", inlineClass);

        var smallSpinnerClass = (string)GetNonPublicProperty(smallInlineSpinner, "SpinnerClass")!;
        Assert.Contains("spinner-border-sm", smallSpinnerClass);
    }

    [Fact]
    public void EmptyState_DefaultAndAssignedValues()
    {
        var empty = new EmptyState();
        Assert.Equal("folder", empty.IconName);
        Assert.Equal("No items found", empty.Title);
        Assert.Null(empty.Description);
        Assert.Null(empty.ActionContent);

        var customEmpty = new EmptyState
        {
            IconName = "users",
            Title = "No users found",
            Description = "Try adjusting your search criteria"
        };

        Assert.Equal("users", customEmpty.IconName);
        Assert.Equal("No users found", customEmpty.Title);
        Assert.Equal("Try adjusting your search criteria", customEmpty.Description);
    }
}
