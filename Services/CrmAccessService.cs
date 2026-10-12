using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Services;

public class CrmAccessService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    public CrmAccessService(ApplicationDbContext db, UserManager<ApplicationUser> users) { _db = db; _users = users; }

    public async Task<List<ApplicationUser>> SalesUsersAsync()
    {
        var users = await _users.GetUsersInRoleAsync("Sales Executive");
        return users.Where(x => x.IsActive).OrderBy(x => x.DisplayName).ToList();
    }

    public async Task<bool> IsValidOwnerAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var user = await _users.FindByIdAsync(userId);
        return user is { IsActive: true } && await _users.IsInRoleAsync(user, "Sales Executive");
    }

    public string UserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public bool CanSeeAll(ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Manager");

    public IQueryable<Customer> Customers(IQueryable<Customer> query, ClaimsPrincipal user) =>
        CanSeeAll(user) ? query : query.Where(x => x.OwnerId == UserId(user));

    public IQueryable<Lead> Leads(IQueryable<Lead> query, ClaimsPrincipal user) =>
        CanSeeAll(user) ? query : query.Where(x => x.OwnerId == UserId(user));

    public IQueryable<Opportunity> Opportunities(IQueryable<Opportunity> query, ClaimsPrincipal user) =>
        CanSeeAll(user) ? query : query.Where(x => x.OwnerId == UserId(user));

    public IQueryable<FollowUp> FollowUps(IQueryable<FollowUp> query, ClaimsPrincipal user) =>
        CanSeeAll(user) ? query : query.Where(x => x.AssignedTo == UserId(user));

    public IQueryable<Activity> Activities(IQueryable<Activity> query, ClaimsPrincipal user) =>
        CanSeeAll(user) ? query : query.Where(x => x.AssignedTo == UserId(user));

    public string DefaultOwner(ClaimsPrincipal user) => UserId(user);
}
