using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager,Sales Executive")]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;
    private readonly CrmAccessService _access;
    public ActivitiesController(ApplicationDbContext context, AuditService audit, CrmAccessService access)
    { _context = context; _audit = audit; _access = access; }

    public async Task<IActionResult> Index(string? search, string? type, string? status, string? assignedTo, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var query = _access.Activities(_context.Activities.AsNoTracking().Include(x => x.Customer).Include(x => x.Lead), User);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Subject.Contains(search) || x.Description.Contains(search) || (x.Customer != null && x.Customer.CustomerName.Contains(search)) || (x.Lead != null && x.Lead.LeadName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.ActivityType == type);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(assignedTo)) query = query.Where(x => x.AssignedTo == assignedTo);
        if (fromDate.HasValue) query = query.Where(x => x.ActivityDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.ActivityDate < toDate.Value.Date.AddDays(1));
        const int pageSize = 20; var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        ViewBag.Search = search; ViewBag.Type = type; ViewBag.Status = status; ViewBag.AssignedTo = assignedTo; ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd"); ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd"); ViewBag.Page = page; ViewBag.PageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (_access.CanSeeAll(User)) ViewBag.OwnerOptions = new SelectList(await _access.SalesUsersAsync(), "Id", "DisplayName", assignedTo);
        return View(await query.OrderByDescending(x => x.ActivityDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await _access.Activities(_context.Activities.AsNoTracking().Include(x => x.Customer).Include(x => x.Lead), User).FirstOrDefaultAsync(x => x.ActivityId == id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create() { await LoadDropdowns(); return View(new Activity()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity item)
    {
        await ValidateAssociation(item); ValidateRules(item);
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(item.AssignedTo)) ModelState.AddModelError(nameof(item.AssignedTo), "Assign this activity to an active Sales Executive.");
        else if (!_access.CanSeeAll(User)) item.AssignedTo = _access.UserId(User);
        if (!ModelState.IsValid) { await LoadDropdowns(item.CustomerId, item.LeadId, item.AssignedTo); return View(item); }
        _context.Activities.Add(item); await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", "Activity", item.ActivityId, $"Created activity: {item.Subject}");
        TempData["Success"] = "Activity created successfully."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await _access.Activities(_context.Activities, User).FirstOrDefaultAsync(x => x.ActivityId == id);
        if (item == null) return NotFound();
        await LoadDropdowns(item.CustomerId, item.LeadId, item.AssignedTo); return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Activity item)
    {
        if (id != item.ActivityId) return NotFound();
        var existing = await _access.Activities(_context.Activities, User).FirstOrDefaultAsync(x => x.ActivityId == id);
        if (existing == null) return NotFound();
        if (!_access.CanSeeAll(User) && existing.AssignedTo != _access.UserId(User)) return Forbid();
        await ValidateAssociation(item); ValidateRules(item);
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(item.AssignedTo)) ModelState.AddModelError(nameof(item.AssignedTo), "Assign this activity to an active Sales Executive.");
        if (!ModelState.IsValid)
        {
            item.AssignedTo = _access.CanSeeAll(User) ? item.AssignedTo : existing.AssignedTo;
            await LoadDropdowns(item.CustomerId, item.LeadId, item.AssignedTo); return View(item);
        }
        var oldStatus = existing.Status; existing.Subject = item.Subject; existing.ActivityType = item.ActivityType;
        existing.Description = item.Description; existing.ActivityDate = item.ActivityDate; existing.Status = item.Status;
        existing.CustomerId = item.CustomerId; existing.LeadId = item.LeadId;
        if (_access.CanSeeAll(User)) existing.AssignedTo = item.AssignedTo;
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", "Activity", id, "Updated activity.", oldValue: oldStatus, newValue: existing.Status);
        TempData["Success"] = "Activity updated successfully."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var item = await _access.Activities(_context.Activities.AsNoTracking().Include(x => x.Customer).Include(x => x.Lead), User).FirstOrDefaultAsync(x => x.ActivityId == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _access.Activities(_context.Activities, User).FirstOrDefaultAsync(x => x.ActivityId == id);
        if (item == null) return NotFound();
        _context.Activities.Remove(item); await _context.SaveChangesAsync();
        await _audit.LogAsync("Delete", "Activity", id, $"Deleted activity: {item.Subject}");
        TempData["Success"] = "Activity deleted successfully."; return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAssociation(Activity item)
    {
        if (item.CustomerId.HasValue == item.LeadId.HasValue) ModelState.AddModelError("", "Select exactly one customer or one lead.");
        if (item.CustomerId.HasValue && !await _access.Customers(_context.Customers, User).AnyAsync(x => x.CustomerId == item.CustomerId)) ModelState.AddModelError(nameof(item.CustomerId), "Select an accessible customer.");
        if (item.LeadId.HasValue && !await _access.Leads(_context.Leads, User).AnyAsync(x => x.LeadId == item.LeadId)) ModelState.AddModelError(nameof(item.LeadId), "Select an accessible lead.");
    }

    private void ValidateRules(Activity item)
    {
        if (!new[] { "Call", "Meeting", "Email", "Task" }.Contains(item.ActivityType, StringComparer.OrdinalIgnoreCase)) ModelState.AddModelError(nameof(item.ActivityType), "Select Call, Meeting, Email, or Task.");
        if (!new[] { "Planned", "In Progress", "Completed", "Cancelled" }.Contains(item.Status, StringComparer.OrdinalIgnoreCase)) ModelState.AddModelError(nameof(item.Status), "Select a valid activity status.");
    }

    private async Task LoadDropdowns(int? customerId = null, int? leadId = null, string? assignedTo = null)
    {
        ViewBag.Customers = new SelectList(await _access.Customers(_context.Customers.AsNoTracking(), User).OrderBy(x => x.CustomerName).ToListAsync(), "CustomerId", "CustomerName", customerId);
        ViewBag.Leads = new SelectList(await _access.Leads(_context.Leads.AsNoTracking(), User).OrderBy(x => x.LeadName).ToListAsync(), "LeadId", "LeadName", leadId);
        ViewBag.Owners = new SelectList(await _access.SalesUsersAsync(), "Id", "DisplayName", assignedTo);
    }
}
