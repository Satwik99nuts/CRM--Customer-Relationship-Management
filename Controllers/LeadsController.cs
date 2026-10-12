using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class LeadsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public LeadsController(
        ApplicationDbContext context,
        AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _context.Leads.AsNoTracking();

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

        return View(await query
            .OrderByDescending(l => l.CreatedDate)
            .ToListAsync());
    }

    public IActionResult Create()
    {
        return View(new Lead());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        if (!ModelState.IsValid)
            return View(lead);

        lead.CreatedDate = DateTime.UtcNow;

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

        var lead = await _context.Leads.FindAsync(id);

        return lead == null ? NotFound() : View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead lead)
    {
        if (id != lead.LeadId)
            return NotFound();

        if (!ModelState.IsValid)
            return View(lead);

        var existing = await _context.Leads.FindAsync(id);

        if (existing == null)
            return NotFound();

        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.ExpectedValue = lead.ExpectedValue;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update",
            "Lead",
            existing.LeadId,
            $"Updated lead: {existing.LeadName}");

        TempData["Success"] = "Lead updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var lead = await _context.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeadId == id);

        return lead == null ? NotFound() : View(lead);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var lead = await _context.Leads
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeadId == id);

        return lead == null ? NotFound() : View(lead);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lead = await _context.Leads.FindAsync(id);

        if (lead == null)
            return NotFound();

        var leadName = lead.LeadName;
        var leadId = lead.LeadId;

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
}