using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager,Sales Executive")]
public class ReportsController : Controller
{
    private const int PageSize = 25;
    private readonly ApplicationDbContext _db;
    private readonly CrmAccessService _access;
    public ReportsController(ApplicationDbContext db, CrmAccessService access) { _db = db; _access = access; }

    public async Task<IActionResult> Index(string report = "customers", string? search = null,
        string? status = null, string? owner = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1)
    {
        report = report.ToLowerInvariant();
        if (report is "audit" && !User.IsInRole("Admin")) return Forbid();
        if (report is "user-activity" && !User.IsInRole("Admin") && !User.IsInRole("Manager")) return Forbid();
        if (toDate.HasValue && fromDate.HasValue && toDate < fromDate)
            ModelState.AddModelError("", "To date must be on or after From date.");
        var all = await LoadRows(report, search, status, owner, fromDate, toDate);
        var model = new ReportViewModel
        {
            Report = report, Search = search, Status = status, Owner = owner,
            FromDate = fromDate, ToDate = toDate, Page = Math.Max(1, page),
            TotalCount = all.Count, PageCount = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize))
        };
        model.Page = Math.Min(model.Page, model.PageCount);
        model.Rows = all.Skip((model.Page - 1) * PageSize).Take(PageSize).ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string report = "customers", string? search = null,
        string? status = null, string? owner = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        report = report.ToLowerInvariant();
        if (report == "audit" && !User.IsInRole("Admin")) return Forbid();
        if (report == "user-activity" && !User.IsInRole("Admin") && !User.IsInRole("Manager")) return Forbid();
        var rows = await LoadRows(report, search, status, owner, fromDate, toDate);
        static string Cell(string value) => "\"" + (value.StartsWith('=') || value.StartsWith('+') || value.StartsWith('-') || value.StartsWith('@') ? "'" : "") + value.Replace("\"", "\"\"") + "\"";
        var csv = "Name,Category,Status,Owner,Related,Amount,Date,Details\r\n" + string.Join("\r\n", rows.Select(row => string.Join(",", new[]
        {
            Cell(row.Name), Cell(row.Category), Cell(row.Status), Cell(row.Owner), Cell(row.Related),
            Cell(row.Amount?.ToString("0.00") ?? ""), Cell(row.Date?.ToString("yyyy-MM-dd HH:mm") ?? ""), Cell(row.Details)
        })));
        return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv", $"{report}-report.csv");
    }

    private async Task<List<ReportRow>> LoadRows(string report, string? search, string? status,
        string? owner, DateTime? fromDate, DateTime? toDate)
    {
        var rows = new List<ReportRow>();
        var start = fromDate?.Date ?? DateTime.MinValue;
        var end = toDate?.Date.AddDays(1) ?? DateTime.MaxValue;
        switch (report)
        {
            case "customers":
                var customers = _access.Customers(_db.Customers.AsNoTracking(), User);
                if (!string.IsNullOrWhiteSpace(search)) customers = customers.Where(x => x.CustomerName.Contains(search) || x.Email.Contains(search) || x.Phone.Contains(search) || x.CompanyName.Contains(search));
                if (!string.IsNullOrWhiteSpace(status)) customers = customers.Where(x => x.Status == status);
                if (!string.IsNullOrWhiteSpace(owner)) customers = customers.Where(x => x.OwnerId == owner);
                rows = await customers.Where(x => x.CreatedDate >= start && x.CreatedDate < end).OrderByDescending(x => x.CreatedDate).Select(x => new ReportRow { Name = x.CustomerName, Category = "Customer", Status = x.Status, Owner = x.OwnerId, Amount = null, Date = x.CreatedDate, Details = $"{x.Email} · {x.Phone}" }).ToListAsync();
                break;
            case "leads":
                var leads = _access.Leads(_db.Leads.AsNoTracking(), User);
                if (!string.IsNullOrWhiteSpace(search)) leads = leads.Where(x => x.LeadName.Contains(search) || x.CompanyName.Contains(search) || x.Email.Contains(search));
                if (!string.IsNullOrWhiteSpace(status)) leads = leads.Where(x => x.Status == status);
                if (!string.IsNullOrWhiteSpace(owner)) leads = leads.Where(x => x.OwnerId == owner);
                rows = await leads.Where(x => x.CreatedDate >= start && x.CreatedDate < end).OrderByDescending(x => x.CreatedDate).Select(x => new ReportRow { Name = x.LeadName, Category = x.Source, Status = x.Status, Owner = x.OwnerId, Amount = x.ExpectedValue, Date = x.CreatedDate, Related = x.ConvertedCustomerId.HasValue ? $"Customer {x.ConvertedCustomerId}" : "", Details = x.CompanyName }).ToListAsync();
                break;
            case "conversion":
                var conversionLeads = _access.Leads(_db.Leads.AsNoTracking(), User);
                if (!string.IsNullOrWhiteSpace(search)) conversionLeads = conversionLeads.Where(x => x.LeadName.Contains(search) || x.CompanyName.Contains(search) || x.Email.Contains(search));
                if (!string.IsNullOrWhiteSpace(status)) conversionLeads = conversionLeads.Where(x => x.Status == status);
                if (!string.IsNullOrWhiteSpace(owner)) conversionLeads = conversionLeads.Where(x => x.OwnerId == owner);
                var conversionData = await conversionLeads.Where(x => x.CreatedDate >= start && x.CreatedDate < end)
                    .GroupBy(x => x.OwnerId)
                    .Select(group => new { Owner = group.Key, Total = group.Count(), Converted = group.Count(x => x.Status == "Converted") })
                    .OrderBy(x => x.Owner).ToListAsync();
                rows = conversionData.Select(x => new ReportRow
                {
                    Name = x.Owner, Category = "Lead conversion", Status = $"{x.Converted} converted / {x.Total} leads",
                    Owner = x.Owner, Amount = null,
                    Details = $"Conversion rate: {(x.Total == 0 ? 0 : (decimal)x.Converted * 100m / x.Total):0.0}%"
                }).ToList();
                break;
            case "follow-ups":
                var followUps = _access.FollowUps(_db.FollowUps.AsNoTracking().Include(x => x.Customer).Include(x => x.Lead).Include(x => x.Opportunity), User);
                if (!string.IsNullOrWhiteSpace(search)) followUps = followUps.Where(x => x.Subject.Contains(search) || x.Remarks.Contains(search));
                if (!string.IsNullOrWhiteSpace(status)) followUps = followUps.Where(x => x.Status == status);
                if (!string.IsNullOrWhiteSpace(owner)) followUps = followUps.Where(x => x.AssignedTo == owner);
                rows = await followUps.Where(x => x.FollowUpDate >= start && x.FollowUpDate < end).OrderBy(x => x.FollowUpDate).Select(x => new ReportRow { Name = x.Subject, Category = x.Type, Status = x.Status, Owner = x.AssignedTo, Date = x.FollowUpDate, Related = x.Customer != null ? x.Customer.CustomerName : x.Lead != null ? x.Lead.LeadName : x.Opportunity!.OpportunityName, Details = x.Remarks }).ToListAsync();
                break;
            case "opportunities":
            case "pipeline":
                var opportunities = _access.Opportunities(_db.Opportunities.AsNoTracking().Include(x => x.Customer), User);
                if (!string.IsNullOrWhiteSpace(search)) opportunities = opportunities.Where(x => x.OpportunityName.Contains(search) || x.Stage.Contains(search) || (x.Customer != null && x.Customer.CustomerName.Contains(search)));
                if (!string.IsNullOrWhiteSpace(status)) opportunities = opportunities.Where(x => x.Status == status || x.Stage == status);
                if (!string.IsNullOrWhiteSpace(owner)) opportunities = opportunities.Where(x => x.OwnerId == owner);
                rows = await opportunities.Where(x => x.CreatedDate >= start && x.CreatedDate < end).OrderBy(x => x.Stage).ThenByDescending(x => x.Amount).Select(x => new ReportRow { Name = x.OpportunityName, Category = x.Stage, Status = x.Status, Owner = x.OwnerId, Amount = x.Amount, Date = x.ExpectedCloseDate, Related = x.Customer!.CustomerName, Details = $"Probability {x.Probability}% · Weighted {(x.Amount * x.Probability / 100m):0.00}" }).ToListAsync();
                break;
            case "audit":
                var logs = _db.AuditLogs.AsNoTracking().AsQueryable();
                if (!User.IsInRole("Admin")) logs = logs.Where(x => x.UserId == _access.UserId(User));
                if (!string.IsNullOrWhiteSpace(search)) logs = logs.Where(x => x.Action.Contains(search) || x.EntityName.Contains(search) || x.Details.Contains(search));
                if (!string.IsNullOrWhiteSpace(status)) logs = logs.Where(x => x.Result == status);
                if (!string.IsNullOrWhiteSpace(owner)) logs = logs.Where(x => x.UserId == owner);
                rows = await logs.Where(x => x.CreatedDate >= start && x.CreatedDate < end).OrderByDescending(x => x.CreatedDate).Select(x => new ReportRow { Name = x.Action, Category = x.EntityName, Status = x.Result, Owner = x.UserId, Date = x.CreatedDate, Related = x.RecordId.HasValue ? x.RecordId.ToString()! : "", Details = x.Details }).ToListAsync();
                break;
            case "user-activity":
                var activityLogs = _db.AuditLogs.AsNoTracking().AsQueryable();
                if (!User.IsInRole("Admin")) activityLogs = activityLogs.Where(x => x.UserId == _access.UserId(User));
                if (!string.IsNullOrWhiteSpace(search)) activityLogs = activityLogs.Where(x => x.Action.Contains(search) || x.EntityName.Contains(search) || x.Details.Contains(search));
                if (!string.IsNullOrWhiteSpace(status)) activityLogs = activityLogs.Where(x => x.Result == status);
                if (!string.IsNullOrWhiteSpace(owner)) activityLogs = activityLogs.Where(x => x.UserId == owner);
                var activityData = await activityLogs.Where(x => x.CreatedDate >= start && x.CreatedDate < end)
                    .GroupBy(x => x.UserId)
                    .Select(group => new { User = group.Key, Count = group.Count(), LastAction = group.Max(x => x.CreatedDate) })
                    .OrderByDescending(x => x.Count).ToListAsync();
                rows = activityData.Select(x => new ReportRow
                {
                    Name = x.User, Category = "User activity", Status = $"{x.Count} actions",
                    Owner = x.User, Date = x.LastAction, Details = $"{x.Count} recorded actions in this period"
                }).ToList();
                break;
            default:
                rows = new List<ReportRow>();
                break;
        }
        return rows;
    }
}
