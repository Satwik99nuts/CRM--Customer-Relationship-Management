
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public FollowUpsController(
        ApplicationDbContext context,
        AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Customers = new SelectList(
            await _context.Customers
                .AsNoTracking()
                .OrderBy(c => c.CustomerName)
                .ToListAsync(),
            "CustomerId",
            "CustomerName");

        ViewBag.Leads = new SelectList(
            await _context.Leads
                .AsNoTracking()
                .OrderBy(l => l.LeadName)
                .ToListAsync(),
            "LeadId",
            "LeadName");
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.FollowUps
            .AsNoTracking()
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f =>
                f.Subject.Contains(search) ||
                f.Type.Contains(search) ||
                f.Status.Contains(search) ||
                (f.Remarks != null && f.Remarks.Contains(search)));
        }

        ViewData["Search"] = search;

        return View(await query
            .OrderBy(f => f.FollowUpDate)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var followUp = await _context.FollowUps
            .AsNoTracking()
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

        if (followUp == null)
            return NotFound();

        return View(followUp);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new FollowUp());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        ValidateAssociation(followUp);

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(followUp);
        }

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "FollowUp",
            followUp.FollowUpId,
            $"Scheduled follow-up: {followUp.Subject}");

        TempData["Success"] = "Follow-up scheduled successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var followUp = await _context.FollowUps.FindAsync(id);

        if (followUp == null)
            return NotFound();

        await LoadDropdowns();
        return View(followUp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUp followUp)
    {
        if (id != followUp.FollowUpId)
            return NotFound();

        ValidateAssociation(followUp);

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(followUp);
        }

        var existing = await _context.FollowUps.FindAsync(id);

        if (existing == null)
            return NotFound();

        existing.Subject = followUp.Subject;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.Type = followUp.Type;
        existing.Status = followUp.Status;
        existing.Remarks = followUp.Remarks;
        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update",
            "FollowUp",
            existing.FollowUpId,
            $"Updated follow-up: {existing.Subject}");

        TempData["Success"] = "Follow-up updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var followUp = await _context.FollowUps
            .AsNoTracking()
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

        if (followUp == null)
            return NotFound();

        return View(followUp);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var followUp = await _context.FollowUps.FindAsync(id);

        if (followUp == null)
            return NotFound();

        var subject = followUp.Subject;

        _context.FollowUps.Remove(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete",
            "FollowUp",
            id,
            $"Deleted follow-up: {subject}");

        TempData["Success"] = "Follow-up deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private void ValidateAssociation(FollowUp followUp)
    {
        if (followUp.CustomerId == null && followUp.LeadId == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Associate this follow-up with a customer or a lead.");
        }

        if (followUp.CustomerId.HasValue &&
            !_context.Customers.Any(c =>
                c.CustomerId == followUp.CustomerId.Value))
        {
            ModelState.AddModelError(
                nameof(followUp.CustomerId),
                "Select a valid customer.");
        }

        if (followUp.LeadId.HasValue &&
            !_context.Leads.Any(l =>
                l.LeadId == followUp.LeadId.Value))
        {
            ModelState.AddModelError(
                nameof(followUp.LeadId),
                "Select a valid lead.");
        }
    }
}
