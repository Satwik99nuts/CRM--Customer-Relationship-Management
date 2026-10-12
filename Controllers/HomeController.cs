using AcxiomCRM.Data;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager,Sales Executive")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly CrmAccessService _access;

    public HomeController(ApplicationDbContext context, CrmAccessService access)
    {
        _context = context;
        _access = access;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            ModelState.AddModelError("", "From date must be on or before To date.");

        var model = new DashboardViewModel { FromDate = fromDate, ToDate = toDate };
        var customers = _access.Customers(_context.Customers.AsNoTracking(), User);
        var leads = _access.Leads(_context.Leads.AsNoTracking(), User);
        var opportunities = _access.Opportunities(_context.Opportunities.AsNoTracking(), User);
        var followUps = _access.FollowUps(_context.FollowUps.AsNoTracking(), User);
        var activities = _access.Activities(_context.Activities.AsNoTracking(), User);

        if (fromDate.HasValue)
        {
            var start = fromDate.Value.Date;
            customers = customers.Where(x => x.CreatedDate >= start);
            leads = leads.Where(x => x.CreatedDate >= start);
            opportunities = opportunities.Where(x => x.CreatedDate >= start);
            followUps = followUps.Where(x => x.FollowUpDate >= start);
            activities = activities.Where(x => x.ActivityDate >= start);
        }
        if (toDate.HasValue)
        {
            var end = toDate.Value.Date.AddDays(1);
            customers = customers.Where(x => x.CreatedDate < end);
            leads = leads.Where(x => x.CreatedDate < end);
            opportunities = opportunities.Where(x => x.CreatedDate < end);
            followUps = followUps.Where(x => x.FollowUpDate < end);
            activities = activities.Where(x => x.ActivityDate < end);
        }

        model.TotalCustomers = await customers.CountAsync();
        model.TotalLeads = await leads.CountAsync();
        model.OpenLeads = await leads.CountAsync(x => x.Status != "Lost" && x.Status != "Unqualified" && x.Status != "Converted");
        model.TotalOpportunities = await opportunities.CountAsync();
        model.OpenOpportunities = await opportunities.CountAsync(x => x.Status == "Open" && x.Stage != "Won" && x.Stage != "Lost");
        model.WonOpportunities = await opportunities.CountAsync(x => x.Status == "Won" || x.Stage == "Won");
        model.LostOpportunities = await opportunities.CountAsync(x => x.Status == "Lost" || x.Stage == "Lost");
        var open = opportunities.Where(x => x.Status == "Open" && x.Stage != "Won" && x.Stage != "Lost");
        model.PipelineValue = await open.SumAsync(x => (decimal?)x.Amount) ?? 0m;
        model.WeightedPipeline = await open.SumAsync(x => (decimal?)(x.Amount * x.Probability / 100m)) ?? 0m;
        model.PendingFollowUps = await followUps.CountAsync(x => x.Status == "Planned");
        model.OverdueFollowUps = await followUps.CountAsync(x => x.Status == "Planned" && x.FollowUpDate < DateTime.Today);
        model.CompletedActivities = await activities.CountAsync(x => x.Status == "Completed");
        model.Stages = await opportunities.GroupBy(x => x.Stage)
            .Select(group => new StageSummary { Stage = group.Key, Count = group.Count(), Value = group.Sum(x => x.Amount) })
            .OrderBy(x => x.Stage).ToListAsync();
        model.LeadStatuses = await leads.GroupBy(x => x.Status)
            .Select(group => new LeadStatusSummary { Status = group.Key, Count = group.Count() })
            .OrderBy(x => x.Status).ToListAsync();
        var firstMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
        var won = opportunities.Where(x => (x.Status == "Won" || x.Stage == "Won") && x.CreatedDate >= firstMonth);
        var monthly = await won.GroupBy(x => new { x.CreatedDate.Year, x.CreatedDate.Month })
            .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(x => x.Amount) })
            .ToListAsync();
        model.MonthlySales = Enumerable.Range(0, 6).Select(i => firstMonth.AddMonths(i))
            .Select(month => new MonthlySalesSummary
            {
                Month = month.ToString("MMM yyyy"),
                Amount = monthly.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month)?.Amount ?? 0m
            }).ToList();
        model.RecentActivities = await activities.Include(x => x.Customer).Include(x => x.Lead)
            .OrderByDescending(x => x.ActivityDate).Take(5).ToListAsync();

        return View(model);
    }

    public IActionResult Privacy() => View();

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Models.ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
