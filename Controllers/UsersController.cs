using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private static readonly string[] AllowedRoles = { "Admin", "Manager", "Sales Executive" };
    private readonly UserManager<ApplicationUser> _users;
    private readonly AuditService _audit;

    public UsersController(UserManager<ApplicationUser> users, AuditService audit)
    {
        _users = users;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = _users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.DisplayName.Contains(search) || (x.Email != null && x.Email.Contains(search)));
        const int pageSize = 25;
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var users = await query.OrderBy(x => x.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var rows = new List<UserRowViewModel>();
        foreach (var user in users)
        {
            rows.Add(new UserRowViewModel
            {
                Id = user.Id, DisplayName = user.DisplayName, Email = user.Email ?? "",
                Roles = string.Join(", ", await _users.GetRolesAsync(user)),
                IsActive = user.IsActive, IsLockedOut = await _users.IsLockedOutAsync(user)
            });
        }
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateUserViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!AllowedRoles.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Choose a valid role.");
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(), Email = model.Email.Trim(),
            DisplayName = model.DisplayName.Trim(), EmailConfirmed = true,
            LockoutEnabled = true, IsActive = true
        };
        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        var roleResult = await _users.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            foreach (var error in roleResult.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        await _audit.LogAsync("Create", "User", details: "Created application user.", newValue: $"UserId={user.Id}; Role={model.Role}");
        TempData["Success"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        var user = id == null ? null : await _users.FindByIdAsync(id);
        if (user == null) return NotFound();
        return View(new EditUserViewModel
        {
            Id = user.Id, DisplayName = user.DisplayName, Email = user.Email ?? "",
            Role = (await _users.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive",
            IsActive = user.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!AllowedRoles.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Choose a valid role.");
        var user = await _users.FindByIdAsync(model.Id);
        if (user == null) return NotFound();
        var oldRoles = await _users.GetRolesAsync(user);
        var oldRole = string.Join(", ", oldRoles);
        if (user.Id == _users.GetUserId(User) && (!model.IsActive || model.Role != "Admin"))
            ModelState.AddModelError("", "You cannot deactivate yourself or remove your own Admin role.");
        if (oldRoles.Contains("Admin") && model.Role != "Admin")
        {
            var otherAdmins = await _users.Users.Where(x => x.IsActive && x.Id != user.Id).ToListAsync();
            var activeAdminExists = false;
            foreach (var candidate in otherAdmins)
                activeAdminExists |= await _users.IsInRoleAsync(candidate, "Admin");
            if (!activeAdminExists)
                ModelState.AddModelError(nameof(model.Role), "At least one active Admin account must remain.");
        }
        if (oldRoles.Contains("Admin") && !model.IsActive)
        {
            var otherAdmins = await _users.Users.Where(x => x.IsActive && x.Id != user.Id).ToListAsync();
            var activeAdminExists = false;
            foreach (var candidate in otherAdmins)
                activeAdminExists |= await _users.IsInRoleAsync(candidate, "Admin");
            if (!activeAdminExists)
                ModelState.AddModelError(nameof(model.IsActive), "At least one active Admin account must remain.");
        }
        if (!ModelState.IsValid) return View(model);

        user.DisplayName = model.DisplayName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.IsActive = model.IsActive;
        user.LockoutEnabled = true;
        user.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;
        if (model.IsActive) await _users.ResetAccessFailedCountAsync(user);
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        if (oldRoles.Count != 1 || oldRoles[0] != model.Role)
        {
            var removeResult = await _users.RemoveFromRolesAsync(user, oldRoles);
            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }
            var addResult = await _users.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
            {
                foreach (var role in oldRoles) await _users.AddToRoleAsync(user, role);
                foreach (var error in addResult.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }
            await _audit.LogAsync("RoleChange", "User", details: "Changed primary role.", oldValue: oldRole, newValue: model.Role);
        }
        await _users.UpdateSecurityStampAsync(user);
        await _audit.LogAsync("Update", "User", details: "Updated account profile and status.", newValue: $"UserId={user.Id}; Active={user.IsActive}");
        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string? id)
    {
        if (id == null || await _users.FindByIdAsync(id) == null) return NotFound();
        return View(new ResetPasswordViewModel { UserId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var user = await _users.FindByIdAsync(model.UserId);
        if (user == null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        await _audit.LogAsync("PasswordReset", "User", details: $"Administrator reset account password for user {user.Id}; password contents omitted.");
        TempData["Success"] = "Password reset successfully.";
        return RedirectToAction(nameof(Index));
    }
}

public class UserRowViewModel
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Roles { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
}
