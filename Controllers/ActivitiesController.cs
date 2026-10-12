
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public ActivitiesController(
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
        var query = _context.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.Subject.Contains(search) ||
                a.ActivityType.Contains(search) ||
                a.Status.Contains(search) ||
                (a.Description != null &&
                 a.Description.Contains(search)));
        }

        ViewData["Search"] = search;

        return View(await query
            .OrderByDescending(a => a.ActivityDate)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var activity = await _context.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .FirstOrDefaultAsync(a => a.ActivityId == id);

        if (activity == null)
            return NotFound();

        return View(activity);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new Activity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity activity)
    {
        ValidateAssociation(activity);

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(activity);
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "Activity",
            activity.ActivityId,
            $"Created activity: {activity.Subject}");

        TempData["Success"] = "Activity created successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var activity = await _context.Activities.FindAsync(id);

        if (activity == null)
            return NotFound();

        await LoadDropdowns();
        return View(activity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Activity activity)
    {
        if (id != activity.ActivityId)
            return NotFound();

        ValidateAssociation(activity);

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View(activity);
        }

        var existing = await _context.Activities.FindAsync(id);

        if (existing == null)
            return NotFound();

        existing.Subject = activity.Subject;
        existing.ActivityType = activity.ActivityType;
        existing.Description = activity.Description;
        existing.ActivityDate = activity.ActivityDate;
        existing.Status = activity.Status;
        existing.CustomerId = activity.CustomerId;
        existing.LeadId = activity.LeadId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update",
            "Activity",
            existing.ActivityId,
            $"Updated activity: {existing.Subject}");

        TempData["Success"] = "Activity updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var activity = await _context.Activities
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .FirstOrDefaultAsync(a => a.ActivityId == id);

        if (activity == null)
            return NotFound();

        return View(activity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var activity = await _context.Activities.FindAsync(id);

        if (activity == null)
            return NotFound();

        var subject = activity.Subject;

        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete",
            "Activity",
            id,
            $"Deleted activity: {subject}");

        TempData["Success"] = "Activity deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private void ValidateAssociation(Activity activity)
    {
        if (activity.CustomerId == null && activity.LeadId == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Associate this activity with a customer or a lead.");
        }

        if (activity.CustomerId.HasValue &&
            !_context.Customers.Any(c =>
                c.CustomerId == activity.CustomerId.Value))
        {
            ModelState.AddModelError(
                nameof(activity.CustomerId),
                "Select a valid customer.");
        }

        if (activity.LeadId.HasValue &&
            !_context.Leads.Any(l =>
                l.LeadId == activity.LeadId.Value))
        {
            ModelState.AddModelError(
                nameof(activity.LeadId),
                "Select a valid lead.");
        }
    }
}
