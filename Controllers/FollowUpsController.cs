using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager,Sales Executive")]
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;
    private readonly CrmAccessService _access;
    public FollowUpsController(ApplicationDbContext context, AuditService audit, CrmAccessService access)
    { _context = context; _audit = audit; _access = access; }

    public async Task<IActionResult> Index(string? search, string? status, string? assignedTo, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var query = _access.FollowUps(_context.FollowUps.AsNoTracking().Include(f => f.Customer).Include(f => f.Lead).Include(f => f.Opportunity), User);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(f => f.Subject.Contains(search) || f.Type.Contains(search) || f.Status.Contains(search) || f.Remarks.Contains(search) || (f.Customer != null && f.Customer.CustomerName.Contains(search)) || (f.Lead != null && f.Lead.LeadName.Contains(search)) || (f.Opportunity != null && f.Opportunity.OpportunityName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(f => f.Status == status);
        if (!string.IsNullOrWhiteSpace(assignedTo)) query = query.Where(f => f.AssignedTo == assignedTo);
        if (fromDate.HasValue) query = query.Where(f => f.FollowUpDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(f => f.FollowUpDate < toDate.Value.Date.AddDays(1));
        const int pageSize = 20;
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        ViewBag.Search = search; ViewBag.Status = status; ViewBag.AssignedTo = assignedTo; ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd"); ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        if (_access.CanSeeAll(User)) ViewBag.OwnerOptions = new SelectList(await _access.SalesUsersAsync(), "Id", "DisplayName", assignedTo);
        ViewBag.Page = page; ViewBag.PageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.Overdue = await query.CountAsync(f => f.Status == "Planned" && f.FollowUpDate < DateTime.Today);
        return View(await query.OrderBy(f => f.FollowUpDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await _access.FollowUps(_context.FollowUps.AsNoTracking().Include(f => f.Customer).Include(f => f.Lead).Include(f => f.Opportunity), User)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new FollowUp { FollowUpDate = DateTime.Today.AddDays(1) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        await ValidateAssociation(followUp);
        ValidateRules(followUp);
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(followUp.AssignedTo))
            ModelState.AddModelError(nameof(followUp.AssignedTo), "Assign this follow-up to an active Sales Executive.");
        else if (!_access.CanSeeAll(User)) followUp.AssignedTo = _access.UserId(User);
        if (!ModelState.IsValid) { await LoadDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo); return View(followUp); }
        _context.FollowUps.Add(followUp); await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", "FollowUp", followUp.FollowUpId, $"Scheduled follow-up: {followUp.Subject}");
        TempData["Success"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await _access.FollowUps(_context.FollowUps, User).FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (item == null) return NotFound();
        await LoadDropdowns(item.CustomerId, item.LeadId, item.OpportunityId, item.AssignedTo);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUp followUp)
    {
        if (id != followUp.FollowUpId) return NotFound();
        var existing = await _access.FollowUps(_context.FollowUps, User).FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (existing == null) return NotFound();
        if (!_access.CanSeeAll(User) && existing.AssignedTo != _access.UserId(User)) return Forbid();
        await ValidateAssociation(followUp);
        ValidateRules(followUp);
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(followUp.AssignedTo))
            ModelState.AddModelError(nameof(followUp.AssignedTo), "Assign this follow-up to an active Sales Executive.");
        if (!ModelState.IsValid)
        {
            followUp.AssignedTo = _access.CanSeeAll(User) ? followUp.AssignedTo : existing.AssignedTo;
            await LoadDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
            return View(followUp);
        }
        var oldStatus = existing.Status; var oldDate = existing.FollowUpDate;
        existing.Subject = followUp.Subject; existing.FollowUpDate = followUp.FollowUpDate; existing.Type = followUp.Type;
        existing.Status = followUp.Status; existing.Remarks = followUp.Remarks; existing.CustomerId = followUp.CustomerId; existing.LeadId = followUp.LeadId; existing.OpportunityId = followUp.OpportunityId;
        if (_access.CanSeeAll(User)) existing.AssignedTo = followUp.AssignedTo;
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", "FollowUp", id, "Updated follow-up.", oldValue: $"{oldStatus}; {oldDate:O}", newValue: $"{existing.Status}; {existing.FollowUpDate:O}");
        TempData["Success"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var item = await _access.FollowUps(_context.FollowUps.AsNoTracking().Include(f => f.Customer).Include(f => f.Lead).Include(f => f.Opportunity), User).FirstOrDefaultAsync(f => f.FollowUpId == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _access.FollowUps(_context.FollowUps, User).FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (item == null) return NotFound();
        _context.FollowUps.Remove(item); await _context.SaveChangesAsync();
        await _audit.LogAsync("Delete", "FollowUp", id, $"Deleted follow-up: {item.Subject}");
        TempData["Success"] = "Follow-up deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAssociation(FollowUp item)
    {
        var selectedCount = (item.CustomerId.HasValue ? 1 : 0) + (item.LeadId.HasValue ? 1 : 0) + (item.OpportunityId.HasValue ? 1 : 0);
        if (selectedCount != 1) ModelState.AddModelError("", "Select exactly one customer, lead, or opportunity.");
        if (item.CustomerId.HasValue && !await _access.Customers(_context.Customers, User).AnyAsync(x => x.CustomerId == item.CustomerId))
            ModelState.AddModelError(nameof(item.CustomerId), "Select an accessible customer.");
        if (item.LeadId.HasValue && !await _access.Leads(_context.Leads, User).AnyAsync(x => x.LeadId == item.LeadId))
            ModelState.AddModelError(nameof(item.LeadId), "Select an accessible lead.");
        if (item.OpportunityId.HasValue && !await _access.Opportunities(_context.Opportunities, User).AnyAsync(x => x.OpportunityId == item.OpportunityId))
            ModelState.AddModelError(nameof(item.OpportunityId), "Select an accessible opportunity.");
    }

    private void ValidateRules(FollowUp item)
    {
        if (!new[] { "Planned", "Completed", "Missed", "Cancelled" }.Contains(item.Status, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(item.Status), "Select a valid follow-up status.");
        if (!new[] { "Call", "Meeting", "Email", "Task" }.Contains(item.Type, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(item.Type), "Select a valid follow-up type.");
        if (item.Status == "Planned" && item.FollowUpDate.Date < DateTime.Today)
            ModelState.AddModelError(nameof(item.FollowUpDate), "Follow-up date cannot be earlier than today.");
    }

    private async Task LoadDropdowns(int? customerId = null, int? leadId = null, int? opportunityId = null, string? ownerId = null)
    {
        ViewBag.Customers = new SelectList(await _access.Customers(_context.Customers.AsNoTracking(), User).OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName", customerId);
        ViewBag.Leads = new SelectList(await _access.Leads(_context.Leads.AsNoTracking(), User).OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName", leadId);
        ViewBag.Opportunities = new SelectList(await _access.Opportunities(_context.Opportunities.AsNoTracking(), User).OrderBy(o => o.OpportunityName).ToListAsync(), "OpportunityId", "OpportunityName", opportunityId);
        ViewBag.Owners = new SelectList(await _access.SalesUsersAsync(), "Id", "DisplayName", ownerId);
    }
}
