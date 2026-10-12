using AcxiomCRM.Data;using AcxiomCRM.Services;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[ApiController,Authorize(Roles="Admin,Manager,Sales Executive"),Route("api/reports")]
public class ApiReportsController:ControllerBase
{
 private readonly ApplicationDbContext _db;private readonly CrmAccessService _access;
 public ApiReportsController(ApplicationDbContext db,CrmAccessService access){_db=db;_access=access;}
 [HttpGet("pipeline")]
 public async Task<IActionResult> Pipeline(){if(!User.IsInRole("Admin")&&!User.IsInRole("Manager")&&!User.IsInRole("Sales Executive"))return Forbid();var q=_access.Opportunities(_db.Opportunities.AsNoTracking(),User);var stages=await q.GroupBy(x=>new{x.Stage,x.OwnerId}).Select(g=>new{g.Key.Stage,g.Key.OwnerId,Count=g.Count(),Amount=g.Sum(x=>x.Amount),WeightedAmount=g.Sum(x=>x.Amount*x.Probability/100m)}).OrderBy(x=>x.Stage).ToListAsync();return Ok(stages);}
}
