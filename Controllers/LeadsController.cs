using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager,Sales Executive")]
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;
    private readonly CrmAccessService _access;

    public LeadsController(
        ApplicationDbContext context,
        AuditService auditService, CrmAccessService access)
    {
        _context = context;
        _auditService = auditService;
        _access = access;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var query = _access.Leads(_context.Leads.AsNoTracking(), User);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l =>
                l.LeadName.Contains(search) ||
                l.Email.Contains(search) ||
                l.CompanyName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;
        const int pageSize = 20;
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        ViewBag.Page = page;
        ViewBag.PageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        return View(await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadOwners();
        return View(new Lead());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        ValidateLead(lead);
        if (lead.Status != "New") ModelState.AddModelError(nameof(lead.Status), "New leads must start in the New status.");
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(lead.OwnerId))
            ModelState.AddModelError(nameof(lead.OwnerId), "Assign the lead to an active Sales Executive.");
        else if (!_access.CanSeeAll(User)) lead.OwnerId = _access.UserId(User);
        if (!ModelState.IsValid)
        {
            await LoadOwners(lead.OwnerId);
            return View(lead);
        }

        lead.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(lead.LeadCode))
            lead.LeadCode = $"LEA-{DateTime.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "Lead",
            lead.LeadId,
            $"Created lead: {lead.LeadName}");

        TempData["Success"] = "Lead created successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var lead = await _access.Leads(_context.Leads, User).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null) return NotFound();
        await LoadOwners(lead.OwnerId);
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead lead)
    {
        if (id != lead.LeadId)
            return NotFound();

        var existing = await _access.Leads(_context.Leads, User).FirstOrDefaultAsync(l => l.LeadId == id);
        if (existing == null) return NotFound();
        if (!_access.CanSeeAll(User) && existing.OwnerId != _access.UserId(User)) return Forbid();

        ValidateLead(lead);
        if (lead.Status != "Converted" && !IsAllowedTransition(existing.Status, lead.Status))
            ModelState.AddModelError(nameof(lead.Status), $"A lead cannot move from {existing.Status} to {lead.Status}.");
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(lead.OwnerId))
            ModelState.AddModelError(nameof(lead.OwnerId), "Assign the lead to an active Sales Executive.");

        if (!ModelState.IsValid)
        {
            lead.OwnerId = _access.CanSeeAll(User) ? lead.OwnerId : existing.OwnerId;
            await LoadOwners(lead.OwnerId);
            return View(lead);
        }

        var oldStatus = existing.Status;
        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.ExpectedValue = lead.ExpectedValue;
        if (_access.CanSeeAll(User)) existing.OwnerId = lead.OwnerId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update",
            "Lead",
            existing.LeadId,
            $"Updated lead: {existing.LeadName}", oldValue: oldStatus, newValue: existing.Status);

        TempData["Success"] = "Lead updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var lead = await _access.Leads(_context.Leads
            .AsNoTracking()
            , User).Include(l => l.FollowUps).Include(l => l.Activities)
            .Include(l => l.Opportunities).Include(l => l.ConvertedCustomer)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        return lead == null ? NotFound() : View(lead);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var lead = await _access.Leads(_context.Leads
            .AsNoTracking()
            , User).FirstOrDefaultAsync(l => l.LeadId == id);

        return lead == null ? NotFound() : View(lead);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lead = await _access.Leads(_context.Leads, User).FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null)
            return NotFound();

        var leadName = lead.LeadName;
        var leadId = lead.LeadId;

        var hasHistory = await _context.FollowUps.AnyAsync(f => f.LeadId == id)
            || await _context.Activities.AnyAsync(a => a.LeadId == id)
            || await _context.Opportunities.AnyAsync(o => o.LeadId == id)
            || lead.ConvertedCustomerId.HasValue;
        if (hasHistory)
        {
            TempData["Error"] = "This lead has follow-ups, activities or opportunities. Reassign or retain those records before deleting the lead.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete",
            "Lead",
            leadId,
            $"Deleted lead: {leadName}");

        TempData["Success"] = "Lead deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _access.Leads(_context.Leads, User).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null) return NotFound();
        if (lead.Status != "Qualified")
        {
            TempData["Error"] = "Only qualified leads can be converted.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (await _context.Customers.AnyAsync(c => c.Email == lead.Email || c.Phone == lead.Phone))
        {
            TempData["Error"] = "A customer with this lead's email or phone already exists. Update the lead or customer before converting.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var customer = new Customer
        {
            CustomerCode = $"CUS-{DateTime.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            CustomerName = lead.LeadName, Email = lead.Email, Phone = lead.Phone,
            CompanyName = lead.CompanyName, Status = "Active", CreatedDate = DateTime.UtcNow,
            CreatedBy = _access.UserId(User), OwnerId = lead.OwnerId
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        lead.Status = "Converted";
        lead.ConvertedCustomerId = customer.CustomerId;
        lead.ConvertedDate = DateTime.UtcNow;
        if (lead.ExpectedValue > 0)
        {
            _context.Opportunities.Add(new Opportunity
            {
                OpportunityName = $"{lead.CompanyName} - {lead.LeadName}".Trim(' ', '-'),
                CustomerId = customer.CustomerId, LeadId = lead.LeadId, Amount = lead.ExpectedValue,
                Probability = 10, Stage = "Qualification", Status = "Open",
                ExpectedCloseDate = DateTime.Today.AddDays(30), OwnerId = lead.OwnerId
            });
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _auditService.LogAsync("Convert", "Lead", lead.LeadId,
            $"Converted lead {lead.LeadName} to customer {customer.CustomerId}.", newValue: $"CustomerId={customer.CustomerId}");
        TempData["Success"] = "Lead converted to a customer successfully.";
        return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
    }

    private void ValidateLead(Lead lead)
    {
        var statuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
        if (!statuses.Contains(lead.Status, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(lead.Status), "Select a valid lead status.");
        if (lead.Status == "Converted")
            ModelState.AddModelError(nameof(lead.Status), "Use the Convert action to convert a qualified lead.");
        if (lead.ExpectedValue < 0)
            ModelState.AddModelError(nameof(lead.ExpectedValue), "Expected value cannot be negative.");
    }

    private static bool IsAllowedTransition(string current, string next) => current switch
    {
        "New" => next is "New" or "Contacted" or "Qualified" or "Unqualified" or "Lost",
        "Contacted" => next is "Contacted" or "Qualified" or "Unqualified" or "Lost",
        "Qualified" => next is "Qualified" or "Contacted" or "Unqualified" or "Lost",
        "Unqualified" => next == "Unqualified",
        "Lost" => next == "Lost",
        _ => false
    };

    private async Task LoadOwners(string? selected = null) =>
        ViewBag.Owners = new SelectList(await _access.SalesUsersAsync(), "Id", "DisplayName", selected);
}
