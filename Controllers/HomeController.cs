
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalCustomers = await _context.Customers.CountAsync();
        ViewBag.TotalLeads = await _context.Leads.CountAsync();
        ViewBag.TotalOpportunities = await _context.Opportunities.CountAsync();

        ViewBag.PipelineValue = await _context.Opportunities
            .Where(o => o.Status == "Open")
            .SumAsync(o => (decimal?)o.Amount) ?? 0m;

        ViewBag.PendingFollowUps = await _context.FollowUps
            .CountAsync(f => f.Status == "Planned");

        ViewBag.CompletedActivities = await _context.Activities
            .CountAsync(a => a.Status == "Completed");

        ViewBag.StageCounts = await _context.Opportunities
            .GroupBy(o => o.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                Count = g.Count(),
                Value = g.Sum(o => o.Amount)
            })
            .ToListAsync();

        var recentActivities = await _context.Activities
            .AsNoTracking()
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();

        return View(recentActivities);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
