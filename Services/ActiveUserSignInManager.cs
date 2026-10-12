using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AcxiomCRM.Services;

public class ActiveUserSignInManager : SignInManager<ApplicationUser>
{
    private readonly AuditService _audit;
    public ActiveUserSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation,
        AuditService audit)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _audit = audit;
    }

    public override Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (!user.IsActive) return Task.FromResult(false);
        return base.CanSignInAsync(user);
    }

    public override async Task<SignInResult> PasswordSignInAsync(
        string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        var user = await UserManager.FindByNameAsync(userName);
        if (user == null)
        {
            await _audit.LogAsync("LoginFailed", "Authentication", details: "Unknown username.", result: "Failure");
            return SignInResult.Failed;
        }
        return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    }

    public override async Task<SignInResult> PasswordSignInAsync(
        ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure)
    {
        var result = await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        var action = result.Succeeded ? "Login" : result.IsLockedOut ? "AccountLocked" : "LoginFailed";
        await _audit.LogAsync(action, "Authentication",
            details: result.Succeeded ? "User signed in." : result.IsLockedOut ? "Account locked after repeated failures." : "Credential check failed.",
            result: result.Succeeded ? "Success" : "Failure");
        return result;
    }
}
