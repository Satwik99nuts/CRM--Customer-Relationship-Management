
using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? actionFilter,
        string? module,
        string? userId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionFilter))
            query = query.Where(x =>
                x.Action.Contains(actionFilter));

        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(x =>
                x.EntityName.Contains(module));

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x =>
                x.UserId.Contains(userId));

        if (fromDate.HasValue)
        {
            var start = fromDate.Value.Date;
            query = query.Where(x => x.CreatedDate >= start);
        }

        if (toDate.HasValue)
        {
            var end = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedDate < end);
        }

        ViewBag.ActionFilter = actionFilter;
        ViewBag.Module = module;
        ViewBag.UserId = userId;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        var logs = await query
            .OrderByDescending(x => x.CreatedDate)
            .Take(500)
            .ToListAsync();

        return View(logs);
    }
}
