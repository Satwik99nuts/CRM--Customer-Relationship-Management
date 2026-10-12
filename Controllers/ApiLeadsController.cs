using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[ApiController, Authorize(Roles="Admin,Manager,Sales Executive"), Route("api/leads")]
public class ApiLeadsController : ControllerBase
{
    private static readonly string[] Statuses={"New","Contacted","Qualified","Unqualified","Converted","Lost"};
    private readonly ApplicationDbContext _db; private readonly CrmAccessService _access; private readonly AuditService _audit;
    public ApiLeadsController(ApplicationDbContext db, CrmAccessService access, AuditService audit){_db=db;_access=access;_audit=audit;}
    private static LeadDto Map(Lead x)=>new(){Id=x.LeadId,LeadCode=x.LeadCode,Name=x.LeadName,Email=x.Email,Phone=x.Phone,Company=x.CompanyName,Source=x.Source,Status=x.Status,ExpectedValue=x.ExpectedValue,ConvertedCustomerId=x.ConvertedCustomerId,CreatedDate=x.CreatedDate};
    [HttpGet]
    public async Task<IActionResult> Get(string? search=null,string? status=null,int page=1,int pageSize=25){var q=_access.Leads(_db.Leads.AsNoTracking(),User);if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.LeadName.Contains(search)||x.CompanyName.Contains(search)||x.Email.Contains(search));if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status);var total=await q.CountAsync();page=Math.Max(page,1);pageSize=Math.Clamp(pageSize,1,100);var items=await q.OrderByDescending(x=>x.CreatedDate).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new LeadDto{Id=x.LeadId,LeadCode=x.LeadCode,Name=x.LeadName,Email=x.Email,Phone=x.Phone,Company=x.CompanyName,Source=x.Source,Status=x.Status,ExpectedValue=x.ExpectedValue,ConvertedCustomerId=x.ConvertedCustomerId,CreatedDate=x.CreatedDate}).ToListAsync();return Ok(new{items,total,page,pageSize});}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id){var x=await _access.Leads(_db.Leads.AsNoTracking(),User).FirstOrDefaultAsync(x=>x.LeadId==id);return x==null?NotFound():Ok(Map(x));}
    [HttpPost]
    public async Task<IActionResult> Create(LeadInput input){if(!Statuses.Contains(input.Status,StringComparer.OrdinalIgnoreCase)||input.Status=="Converted")return BadRequest(new{error="invalid_status",message="Choose a valid non-converted lead status."});var x=new Lead{LeadCode=$"LEA-{DateTime.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100,999)}",LeadName=input.Name.Trim(),Email=input.Email.Trim(),Phone=input.Phone.Trim(),CompanyName=input.Company,Source=input.Source,Status=input.Status,ExpectedValue=input.ExpectedValue,CreatedDate=DateTime.UtcNow,OwnerId=_access.UserId(User)};_db.Leads.Add(x);await _db.SaveChangesAsync();await _audit.LogAsync("Create","Lead",x.LeadId,"Created via API.");return CreatedAtAction(nameof(GetById),new{id=x.LeadId},Map(x));}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id,LeadInput input){var x=await _access.Leads(_db.Leads,User).FirstOrDefaultAsync(x=>x.LeadId==id);if(x==null)return NotFound();if(!Statuses.Contains(input.Status,StringComparer.OrdinalIgnoreCase)||input.Status=="Converted")return BadRequest(new{error="invalid_status",message="Choose a valid non-converted lead status."});var old=x.Status;x.LeadName=input.Name.Trim();x.Email=input.Email.Trim();x.Phone=input.Phone.Trim();x.CompanyName=input.Company;x.Source=input.Source;x.Status=input.Status;x.ExpectedValue=input.ExpectedValue;await _db.SaveChangesAsync();await _audit.LogAsync("Update","Lead",id,"Updated via API.",oldValue:old,newValue:x.Status);return Ok(Map(x));}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id){var x=await _access.Leads(_db.Leads,User).FirstOrDefaultAsync(x=>x.LeadId==id);if(x==null)return NotFound();var related=await _db.FollowUps.AnyAsync(f=>f.LeadId==id)||await _db.Activities.AnyAsync(a=>a.LeadId==id)||await _db.Opportunities.AnyAsync(o=>o.LeadId==id);if(related)return Conflict(new{error="related_history",message="Reassign or retain linked history before deleting this lead."});_db.Leads.Remove(x);await _db.SaveChangesAsync();await _audit.LogAsync("Delete","Lead",id,"Deleted via API.");return NoContent();}
}
