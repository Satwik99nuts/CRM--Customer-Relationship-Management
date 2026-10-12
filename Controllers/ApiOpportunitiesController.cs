using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[ApiController, Authorize(Roles = "Admin,Manager,Sales Executive"), Route("api/opportunities")]
public class ApiOpportunitiesController : ControllerBase
{
    private static readonly string[] Stages = { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
    private readonly ApplicationDbContext _db;
    private readonly CrmAccessService _access;
    private readonly AuditService _audit;

    public ApiOpportunitiesController(ApplicationDbContext db, CrmAccessService access, AuditService audit)
    { _db = db; _access = access; _audit = audit; }

    private static OpportunityDto Map(Opportunity x) => new()
    {
        Id = x.OpportunityId, Name = x.OpportunityName, CustomerId = x.CustomerId,
        LeadId = x.LeadId, Amount = x.Amount, Probability = x.Probability,
        Stage = x.Stage, Status = x.Status, ExpectedCloseDate = x.ExpectedCloseDate
    };

    [HttpGet]
    public async Task<IActionResult> Get(string? search = null, string? stage = null, int page = 1, int pageSize = 25)
    {
        var query = _access.Opportunities(_db.Opportunities.AsNoTracking().Include(x => x.Customer), User);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.OpportunityName.Contains(search) || x.Stage.Contains(search) || x.Status.Contains(search) || (x.Customer != null && x.Customer.CustomerName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(x => x.Stage == stage);
        var total = await query.CountAsync();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await query.OrderByDescending(x => x.CreatedDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new OpportunityDto
            {
                Id = x.OpportunityId, Name = x.OpportunityName, CustomerId = x.CustomerId,
                LeadId = x.LeadId, Amount = x.Amount, Probability = x.Probability,
                Stage = x.Stage, Status = x.Status, ExpectedCloseDate = x.ExpectedCloseDate
            }).ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _access.Opportunities(_db.Opportunities.AsNoTracking(), User).FirstOrDefaultAsync(x => x.OpportunityId == id);
        return item == null ? NotFound() : Ok(Map(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create(OpportunityInput input)
    {
        if (!Stages.Contains(input.Stage, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "invalid_stage", message = "Choose a valid pipeline stage." });
        if (!IsValidFinalState(input.Status, input.Stage))
            return BadRequest(new { error = "invalid_status", message = "Open, Won, and Lost status must match the pipeline stage." });
        if (IsActive(input.Status, input.Stage) && input.ExpectedCloseDate.Date < DateTime.Today)
            return BadRequest(new { error = "invalid_close_date", message = "Expected close date cannot be in the past." });
        if (!input.CustomerId.HasValue || !await _access.Customers(_db.Customers, User).AnyAsync(x => x.CustomerId == input.CustomerId))
            return BadRequest(new { error = "invalid_customer", message = "Select an accessible customer." });
        if (input.LeadId.HasValue && !await _access.Leads(_db.Leads, User).AnyAsync(x => x.LeadId == input.LeadId))
            return BadRequest(new { error = "invalid_lead", message = "Select an accessible lead." });

        NormalizeOutcome(input);
        var item = new Opportunity
        {
            OpportunityName = input.Name.Trim(), CustomerId = input.CustomerId, LeadId = input.LeadId,
            Amount = input.Amount, Probability = input.Probability, Stage = input.Stage,
            Status = input.Status, ExpectedCloseDate = input.ExpectedCloseDate,
            CreatedDate = DateTime.UtcNow, OwnerId = _access.UserId(User)
        };
        _db.Opportunities.Add(item);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Opportunity", item.OpportunityId, "Created via API.");
        return CreatedAtAction(nameof(GetById), new { id = item.OpportunityId }, Map(item));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OpportunityInput input)
    {
        var item = await _access.Opportunities(_db.Opportunities, User).FirstOrDefaultAsync(x => x.OpportunityId == id);
        if (item == null) return NotFound();
        if (!Stages.Contains(input.Stage, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "invalid_stage", message = "Choose a valid pipeline stage." });
        if (!IsValidFinalState(input.Status, input.Stage))
            return BadRequest(new { error = "invalid_status", message = "Open, Won, and Lost status must match the pipeline stage." });
        if (IsActive(input.Status, input.Stage) && input.ExpectedCloseDate.Date < DateTime.Today)
            return BadRequest(new { error = "invalid_close_date", message = "Expected close date cannot be in the past." });
        if (!input.CustomerId.HasValue || !await _access.Customers(_db.Customers, User).AnyAsync(x => x.CustomerId == input.CustomerId))
            return BadRequest(new { error = "invalid_customer", message = "Select an accessible customer." });
        if (input.LeadId.HasValue && !await _access.Leads(_db.Leads, User).AnyAsync(x => x.LeadId == input.LeadId))
            return BadRequest(new { error = "invalid_lead", message = "Select an accessible lead." });

        NormalizeOutcome(input);
        var old = $"{item.Stage}; {item.Status}; {item.Amount}; {item.Probability}";
        item.OpportunityName = input.Name.Trim(); item.CustomerId = input.CustomerId;
        item.LeadId = input.LeadId; item.Amount = input.Amount; item.Probability = input.Probability;
        item.Stage = input.Stage; item.Status = input.Status; item.ExpectedCloseDate = input.ExpectedCloseDate;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Opportunity", id, "Updated via API.", oldValue: old,
            newValue: $"{item.Stage}; {item.Status}; {item.Amount}; {item.Probability}");
        return Ok(Map(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _access.Opportunities(_db.Opportunities, User).FirstOrDefaultAsync(x => x.OpportunityId == id);
        if (item == null) return NotFound();
        if (await _db.FollowUps.AnyAsync(f => f.OpportunityId == id))
            return Conflict(new { error = "related_history", message = "Retain the opportunity or reassign its follow-ups before deleting it." });
        _db.Opportunities.Remove(item);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", "Opportunity", id, "Deleted via API.");
        return NoContent();
    }

    private static bool IsActive(string status, string stage) => status == "Open" && stage is not "Won" and not "Lost";

    private static bool IsValidFinalState(string status, string stage) =>
        status == "Open"
            ? stage is not "Won" and not "Lost"
            : status == stage && (status is "Won" or "Lost");

    private static void NormalizeOutcome(OpportunityInput input)
    {
        if (input.Stage is "Won" or "Lost") input.Status = input.Stage;
        if (input.Status is "Won" or "Lost") input.Stage = input.Status;
    }
}
