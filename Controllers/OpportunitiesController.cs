
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager,Sales Executive")]
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;
    private readonly CrmAccessService _access;

    public OpportunitiesController(
        ApplicationDbContext context,
        AuditService auditService, CrmAccessService access)
    {
        _context = context;
        _auditService = auditService;
        _access = access;
    }

    public async Task<IActionResult> Index(string? search, string? stage, string? status, int page = 1)
    {
        IQueryable<Opportunity> query = _access.Opportunities(_context.Opportunities
            .AsNoTracking()
            .AsQueryable(), User).Include(o => o.Customer);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.OpportunityName.Contains(search) ||
                o.Stage.Contains(search) ||
                o.Status.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(o => o.Stage == stage);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);

        ViewData["Search"] = search;
        ViewBag.Stage = stage;
        ViewBag.Status = status;
        const int pageSize = 20;
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        ViewBag.Page = page;
        ViewBag.PageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        return View(await query
            .OrderByDescending(o => o.OpportunityId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var opportunity = await _access.Opportunities(_context.Opportunities
            .AsNoTracking()
            , User).Include(o => o.Customer).Include(o => o.Lead).Include(o => o.FollowUps).FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();

        return View(opportunity);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new Opportunity
        {
            Stage = "Qualification",
            Status = "Open",
            Probability = 10,
            ExpectedCloseDate = DateTime.Today.AddDays(30)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        ValidateOpportunity(opportunity);
        await ValidateReferencesAndOwner(opportunity);

        if (!ModelState.IsValid)
        {
            await LoadDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.OwnerId);
            return View(opportunity);
        }

        opportunity.CreatedDate = DateTime.UtcNow;
        opportunity.OwnerId = _access.CanSeeAll(User) ? opportunity.OwnerId : _access.UserId(User);

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "Opportunity",
            opportunity.OpportunityId,
            $"Created opportunity: {opportunity.OpportunityName}");

        TempData["Success"] = "Opportunity created successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var opportunity = await _access.Opportunities(_context.Opportunities, User)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();

        await LoadDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.OwnerId);
        return View(opportunity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Opportunity opportunity)
    {
        if (id != opportunity.OpportunityId)
            return NotFound();

        var existing = await _access.Opportunities(_context.Opportunities, User)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (existing == null) return NotFound();
        if (!_access.CanSeeAll(User) && existing.OwnerId != _access.UserId(User)) return Forbid();

        ValidateOpportunity(opportunity);
        await ValidateReferencesAndOwner(opportunity);

        if (!ModelState.IsValid)
        {
            opportunity.OwnerId = _access.CanSeeAll(User) ? opportunity.OwnerId : existing.OwnerId;
            await LoadDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.OwnerId);
            return View(opportunity);
        }

        var previous = existing.Stage;
        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Status = opportunity.Status;
        if (_access.CanSeeAll(User)) existing.OwnerId = opportunity.OwnerId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update",
            "Opportunity",
            existing.OpportunityId,
            $"Updated opportunity: {existing.OpportunityName}", oldValue: previous, newValue: existing.Stage);

        TempData["Success"] = "Opportunity updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var opportunity = await _access.Opportunities(_context.Opportunities
            .AsNoTracking()
            , User).Include(o => o.Customer).Include(o => o.Lead).FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();

        return View(opportunity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var opportunity = await _access.Opportunities(_context.Opportunities, User)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();

        var opportunityName = opportunity.OpportunityName;

        if (await _context.FollowUps.AnyAsync(f => f.OpportunityId == id))
        {
            TempData["Error"] = "This opportunity has follow-up history. Keep the opportunity or reassign those follow-ups before deleting it.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        _context.Opportunities.Remove(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete",
            "Opportunity",
            id,
            $"Deleted opportunity: {opportunityName}");

        TempData["Success"] = "Opportunity deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private void ValidateOpportunity(Opportunity opportunity)
    {
        if (!opportunity.CustomerId.HasValue)
            ModelState.AddModelError(nameof(opportunity.CustomerId), "Select a customer for this opportunity.");
        if (opportunity.Amount <= 0)
        {
            ModelState.AddModelError(
                nameof(opportunity.Amount),
                "Amount must be greater than zero.");
        }

        if (opportunity.Probability < 0 ||
            opportunity.Probability > 100)
        {
            ModelState.AddModelError(
                nameof(opportunity.Probability),
                "Probability must be between 0 and 100.");
        }

        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        if (!stages.Contains(opportunity.Stage, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(opportunity.Stage), "Select a valid pipeline stage.");
        var statuses = new[] { "Open", "Won", "Lost" };
        if (!statuses.Contains(opportunity.Status, StringComparer.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(opportunity.Status), "Select a valid opportunity status.");
        if (opportunity.Status.Equals("Open", StringComparison.OrdinalIgnoreCase) &&
            opportunity.Stage is not "Won" and not "Lost" && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            ModelState.AddModelError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        if (opportunity.Stage.Equals("Won", StringComparison.OrdinalIgnoreCase)) opportunity.Status = "Won";
        if (opportunity.Stage.Equals("Lost", StringComparison.OrdinalIgnoreCase)) opportunity.Status = "Lost";
        if (opportunity.Status == "Won") opportunity.Stage = "Won";
        if (opportunity.Status == "Lost") opportunity.Stage = "Lost";
        if (opportunity.Status == "Open" && opportunity.Stage is "Won" or "Lost")
            ModelState.AddModelError(nameof(opportunity.Status), "Won/Lost opportunities must use the matching final status.");
    }

    private async Task ValidateReferencesAndOwner(Opportunity opportunity)
    {
        if (opportunity.CustomerId.HasValue &&
            !await _access.Customers(_context.Customers, User).AnyAsync(x => x.CustomerId == opportunity.CustomerId))
            ModelState.AddModelError(nameof(opportunity.CustomerId), "Select an accessible customer.");
        if (opportunity.LeadId.HasValue &&
            !await _access.Leads(_context.Leads, User).AnyAsync(x => x.LeadId == opportunity.LeadId))
            ModelState.AddModelError(nameof(opportunity.LeadId), "Select an accessible lead.");
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(opportunity.OwnerId))
            ModelState.AddModelError(nameof(opportunity.OwnerId), "Assign the opportunity to an active Sales Executive.");
    }

    private async Task LoadDropdowns(int? selectedCustomer = null, int? selectedLead = null, string? selectedOwner = null)
    {
        ViewBag.Customers = new SelectList(await _access.Customers(_context.Customers.AsNoTracking(), User).OrderBy(x => x.CustomerName).ToListAsync(), "CustomerId", "CustomerName", selectedCustomer);
        ViewBag.Leads = new SelectList(await _access.Leads(_context.Leads.AsNoTracking(), User).OrderBy(x => x.LeadName).ToListAsync(), "LeadId", "LeadName", selectedLead);
        ViewBag.Owners = new SelectList(await _access.SalesUsersAsync(), "Id", "DisplayName", selectedOwner);
    }
}
