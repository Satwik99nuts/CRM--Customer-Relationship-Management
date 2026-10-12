using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[ApiController, Authorize(Roles = "Admin,Manager,Sales Executive"), Route("api/customers")]
public class ApiCustomersController : ControllerBase
{
    private readonly ApplicationDbContext _db; private readonly CrmAccessService _access; private readonly AuditService _audit;
    public ApiCustomersController(ApplicationDbContext db, CrmAccessService access, AuditService audit) { _db = db; _access = access; _audit = audit; }
    private static CustomerDto Map(Customer c) => new() { Id=c.CustomerId, CustomerCode=c.CustomerCode, Name=c.CustomerName, Email=c.Email, Phone=c.Phone, Company=c.CompanyName, Address=c.Address, City=c.City, State=c.State, Status=c.Status, CreatedDate=c.CreatedDate };

    [HttpGet]
    public async Task<IActionResult> Get(string? search = null, int page = 1, int pageSize = 25)
    {
        var q = _access.Customers(_db.Customers.AsNoTracking(), User);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CustomerName.Contains(search) || x.Email.Contains(search) || x.Phone.Contains(search) || x.CompanyName.Contains(search));
        var total = await q.CountAsync(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await q.OrderBy(x => x.CustomerName).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new CustomerDto { Id=x.CustomerId, CustomerCode=x.CustomerCode, Name=x.CustomerName, Email=x.Email, Phone=x.Phone, Company=x.CompanyName, Address=x.Address, City=x.City, State=x.State, Status=x.Status, CreatedDate=x.CreatedDate }).ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) { var c = await _access.Customers(_db.Customers.AsNoTracking(), User).FirstOrDefaultAsync(x => x.CustomerId == id); return c == null ? NotFound() : Ok(Map(c)); }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerInput input)
    {
        if (await _db.Customers.AnyAsync(x => x.Email == input.Email || x.Phone == input.Phone)) return Conflict(new { error="duplicate_customer", message="A customer with this email or phone already exists." });
        var userId = _access.UserId(User);
        var c = new Customer { CustomerCode=$"CUS-{DateTime.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100,999)}", CustomerName=input.Name.Trim(), Email=input.Email.Trim(), Phone=input.Phone.Trim(), CompanyName=input.Company, Address=input.Address, City=input.City, State=input.State, Status=input.Status, CreatedDate=DateTime.UtcNow, CreatedBy=userId, OwnerId=userId };
        _db.Customers.Add(c); await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Customer", c.CustomerId, "Created via API.");
        return CreatedAtAction(nameof(GetById), new { id=c.CustomerId }, Map(c));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerInput input)
    {
        var c = await _access.Customers(_db.Customers, User).FirstOrDefaultAsync(x => x.CustomerId == id);
        if (c == null) return NotFound();
        if (await _db.Customers.AnyAsync(x => x.CustomerId != id && (x.Email == input.Email || x.Phone == input.Phone))) return Conflict(new { error="duplicate_customer", message="A customer with this email or phone already exists." });
        var old = $"{c.CustomerName}; {c.Email}; {c.Phone}; {c.Status}";
        c.CustomerName=input.Name.Trim(); c.Email=input.Email.Trim(); c.Phone=input.Phone.Trim(); c.CompanyName=input.Company; c.Address=input.Address; c.City=input.City; c.State=input.State; c.Status=input.Status; c.ModifiedDate=DateTime.UtcNow;
        await _db.SaveChangesAsync(); await _audit.LogAsync("Update", "Customer", id, "Updated via API.", oldValue:old, newValue:$"{c.CustomerName}; {c.Email}; {c.Phone}; {c.Status}");
        return Ok(Map(c));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _access.Customers(_db.Customers, User).FirstOrDefaultAsync(x => x.CustomerId == id);
        if (c == null) return NotFound();
        var related = await _db.Opportunities.AnyAsync(x => x.CustomerId == id) || await _db.FollowUps.AnyAsync(x => x.CustomerId == id) || await _db.Activities.AnyAsync(x => x.CustomerId == id);
        if (related) { c.Status="Inactive"; c.ModifiedDate=DateTime.UtcNow; await _db.SaveChangesAsync(); await _audit.LogAsync("Deactivate", "Customer", id, "Deactivated via API because history exists."); return Ok(new { message="Customer deactivated to preserve related history." }); }
        _db.Customers.Remove(c); await _db.SaveChangesAsync(); await _audit.LogAsync("Delete", "Customer", id, "Deleted via API."); return NoContent();
    }
}
