using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Route("Auth")]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditService _audit;

    public AuthController(SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager, AuditService audit)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
    }

    [AllowAnonymous, HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost("Login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            await _audit.LogAsync("LoginFailed", "Authentication", details: "Invalid credentials or inactive account.", result: "Failure");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl!);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", result.IsLockedOut
            ? "This account is locked. Try again later or contact an administrator."
            : "Invalid email or password.");
        return View(model);
    }

    [AllowAnonymous, HttpGet("Register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost("Register"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(), Email = model.Email.Trim(),
            DisplayName = model.DisplayName.Trim(), EmailConfirmed = true,
            IsActive = true, LockoutEnabled = true
        };
        var created = await _userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors) ModelState.AddModelError("", error.Description);
            await _audit.LogAsync("RegistrationFailed", "Authentication", details: "Registration was rejected.", result: "Failure");
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Sales Executive");
        await _audit.LogAsync("Register", "User", details: "Self-service Sales Executive account created.");
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _audit.LogAsync("Login", "Authentication", details: "New user signed in after registration.");
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost("Logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync("Logout", "Authentication", details: "User signed out.");
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet("AccessDenied")]
    public IActionResult AccessDenied() => View();
}
