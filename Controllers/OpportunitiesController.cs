
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public OpportunitiesController(
        ApplicationDbContext context,
        AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Opportunities
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.OpportunityName.Contains(search) ||
                o.Stage.Contains(search) ||
                o.Status.Contains(search));
        }

        ViewData["Search"] = search;

        return View(await query
            .OrderByDescending(o => o.OpportunityId)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var opportunity = await _context.Opportunities
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();

        return View(opportunity);
    }

    public IActionResult Create()
    {
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

        if (!ModelState.IsValid)
            return View(opportunity);

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

        var opportunity = await _context.Opportunities
            .FindAsync(id);

        if (opportunity == null)
            return NotFound();

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

        ValidateOpportunity(opportunity);

        if (!ModelState.IsValid)
            return View(opportunity);

        var existing = await _context.Opportunities.FindAsync(id);

        if (existing == null)
            return NotFound();

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Status = opportunity.Status;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update",
            "Opportunity",
            existing.OpportunityId,
            $"Updated opportunity: {existing.OpportunityName}");

        TempData["Success"] = "Opportunity updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var opportunity = await _context.Opportunities
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();

        return View(opportunity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var opportunity = await _context.Opportunities.FindAsync(id);

        if (opportunity == null)
            return NotFound();

        var opportunityName = opportunity.OpportunityName;

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
    }
}
