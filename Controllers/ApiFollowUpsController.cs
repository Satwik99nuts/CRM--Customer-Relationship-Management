using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[ApiController, Authorize(Roles = "Admin,Manager,Sales Executive"), Route("api/followups")]
public class ApiFollowUpsController : ControllerBase
{
    private static readonly string[] Statuses = { "Planned", "Completed", "Missed", "Cancelled" };
    private static readonly string[] Types = { "Call", "Meeting", "Email", "Task" };
    private readonly ApplicationDbContext _db;
    private readonly CrmAccessService _access;
    private readonly AuditService _audit;

    public ApiFollowUpsController(ApplicationDbContext db, CrmAccessService access, AuditService audit)
    { _db = db; _access = access; _audit = audit; }

    private static FollowUpDto Map(FollowUp x) => new()
    {
        Id = x.FollowUpId, Subject = x.Subject, FollowUpDate = x.FollowUpDate,
        Type = x.Type, Status = x.Status, CustomerId = x.CustomerId,
        LeadId = x.LeadId, OpportunityId = x.OpportunityId, Remarks = x.Remarks
    };

    [HttpGet]
    public async Task<IActionResult> Get(string? status = null, DateTime? fromDate = null,
        DateTime? toDate = null, int page = 1, int pageSize = 25)
    {
        var query = _access.FollowUps(_db.FollowUps.AsNoTracking(), User);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (fromDate.HasValue) query = query.Where(x => x.FollowUpDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.FollowUpDate < toDate.Value.Date.AddDays(1));
        var total = await query.CountAsync();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await query.OrderBy(x => x.FollowUpDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new FollowUpDto
            {
                Id = x.FollowUpId, Subject = x.Subject, FollowUpDate = x.FollowUpDate,
                Type = x.Type, Status = x.Status, CustomerId = x.CustomerId,
                LeadId = x.LeadId, OpportunityId = x.OpportunityId, Remarks = x.Remarks
            }).ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _access.FollowUps(_db.FollowUps.AsNoTracking(), User).FirstOrDefaultAsync(x => x.FollowUpId == id);
        return item == null ? NotFound() : Ok(Map(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create(FollowUpInput input)
    {
        var selectionCount = (input.CustomerId.HasValue ? 1 : 0) + (input.LeadId.HasValue ? 1 : 0) + (input.OpportunityId.HasValue ? 1 : 0);
        if (selectionCount != 1) return BadRequest(new { error = "invalid_relationship", message = "Choose exactly one customer, lead, or opportunity." });
        if (input.Status == "Planned" && input.FollowUpDate.Date < DateTime.Today)
            return BadRequest(new { error = "invalid_date", message = "Follow-up date cannot be earlier than today." });
        if (!Statuses.Contains(input.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "invalid_status", message = "Choose a valid follow-up status." });
        if (!Types.Contains(input.Type, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "invalid_type", message = "Choose Call, Meeting, Email, or Task." });
        if (input.CustomerId.HasValue && !await _access.Customers(_db.Customers, User).AnyAsync(x => x.CustomerId == input.CustomerId))
            return BadRequest(new { error = "invalid_customer", message = "Select an accessible customer." });
        if (input.LeadId.HasValue && !await _access.Leads(_db.Leads, User).AnyAsync(x => x.LeadId == input.LeadId))
            return BadRequest(new { error = "invalid_lead", message = "Select an accessible lead." });
        if (input.OpportunityId.HasValue && !await _access.Opportunities(_db.Opportunities, User).AnyAsync(x => x.OpportunityId == input.OpportunityId))
            return BadRequest(new { error = "invalid_opportunity", message = "Select an accessible opportunity." });

        var item = new FollowUp
        {
            Subject = input.Subject.Trim(), FollowUpDate = input.FollowUpDate,
            Type = input.Type, Status = input.Status, CustomerId = input.CustomerId,
            LeadId = input.LeadId, OpportunityId = input.OpportunityId,
            Remarks = input.Remarks, AssignedTo = _access.UserId(User)
        };
        _db.FollowUps.Add(item);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "FollowUp", item.FollowUpId, "Created via API.");
        return CreatedAtAction(nameof(GetById), new { id = item.FollowUpId }, Map(item));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FollowUpInput input)
    {
        var item = await _access.FollowUps(_db.FollowUps, User).FirstOrDefaultAsync(x => x.FollowUpId == id);
        if (item == null) return NotFound();
        var validation = await ValidateInput(input);
        if (validation != null) return validation;

        var oldStatus = item.Status;
        item.Subject = input.Subject.Trim();
        item.FollowUpDate = input.FollowUpDate;
        item.Type = input.Type;
        item.Status = input.Status;
        item.CustomerId = input.CustomerId;
        item.LeadId = input.LeadId;
        item.OpportunityId = input.OpportunityId;
        item.Remarks = input.Remarks;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "FollowUp", id, "Updated via API.", oldValue: oldStatus, newValue: item.Status);
        return Ok(Map(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _access.FollowUps(_db.FollowUps, User).FirstOrDefaultAsync(x => x.FollowUpId == id);
        if (item == null) return NotFound();
        _db.FollowUps.Remove(item);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", "FollowUp", id, "Deleted via API.");
        return NoContent();
    }

    private async Task<IActionResult?> ValidateInput(FollowUpInput input)
    {
        var selectionCount = (input.CustomerId.HasValue ? 1 : 0) + (input.LeadId.HasValue ? 1 : 0) + (input.OpportunityId.HasValue ? 1 : 0);
        if (selectionCount != 1) return BadRequest(new { error = "invalid_relationship", message = "Choose exactly one customer, lead, or opportunity." });
        if (!Statuses.Contains(input.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "invalid_status", message = "Choose a valid follow-up status." });
        if (!Types.Contains(input.Type, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "invalid_type", message = "Choose Call, Meeting, Email, or Task." });
        if (input.Status == "Planned" && input.FollowUpDate.Date < DateTime.Today)
            return BadRequest(new { error = "invalid_date", message = "Follow-up date cannot be earlier than today." });
        if (input.CustomerId.HasValue && !await _access.Customers(_db.Customers, User).AnyAsync(x => x.CustomerId == input.CustomerId))
            return BadRequest(new { error = "invalid_customer", message = "Select an accessible customer." });
        if (input.LeadId.HasValue && !await _access.Leads(_db.Leads, User).AnyAsync(x => x.LeadId == input.LeadId))
            return BadRequest(new { error = "invalid_lead", message = "Select an accessible lead." });
        if (input.OpportunityId.HasValue && !await _access.Opportunities(_db.Opportunities, User).AnyAsync(x => x.OpportunityId == input.OpportunityId))
            return BadRequest(new { error = "invalid_opportunity", message = "Select an accessible opportunity." });
        return null;
    }
}
