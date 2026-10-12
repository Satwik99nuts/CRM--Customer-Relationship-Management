using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;

namespace AcxiomCRM.Controllers;

[ApiController, Route("api/auth")]
public class ApiAuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AuditService _audit;
    private readonly IAntiforgery _antiforgery;
    public ApiAuthController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, AuditService audit, IAntiforgery antiforgery)
    { _signIn = signIn; _users = users; _audit = audit; _antiforgery = antiforgery; }

    [AllowAnonymous, HttpGet("antiforgery")]
    public IActionResult AntiforgeryToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [AllowAnonymous, HttpPost("login"), EnableRateLimiting("auth"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(ApiLoginInput input)
    {
        var user = await _users.FindByEmailAsync(input.Email);
        if (user == null || !user.IsActive)
        {
            await _audit.LogAsync("LoginFailed", "Authentication", details: "API login was rejected.", result: "Failure");
            return Unauthorized(new { error = "invalid_credentials", message = "Invalid email or password." });
        }
        var result = await _signIn.PasswordSignInAsync(user, input.Password, false, true);
        if (!result.Succeeded)
        {
            await _audit.LogAsync(result.IsLockedOut ? "AccountLocked" : "LoginFailed", "Authentication", details: "API login was rejected.", result: "Failure");
            return Unauthorized(new { error = result.IsLockedOut ? "account_locked" : "invalid_credentials", message = "Invalid email or password." });
        }
        return Ok(new { userId = user.Id, displayName = user.DisplayName, email = user.Email, roles = await _users.GetRolesAsync(user) });
    }

    [Authorize, HttpPost("logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync("Logout", "Authentication", details: "API user signed out.");
        await _signIn.SignOutAsync();
        return Ok(new { message = "Signed out." });
    }
}
