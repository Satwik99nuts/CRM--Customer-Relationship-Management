using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager,Sales Executive")]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;
    private readonly CrmAccessService _access;

    public CustomersController(ApplicationDbContext context, AuditService audit, CrmAccessService access)
    {
        _context = context;
        _audit = audit;
        _access = access;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var query = _access.Customers(_context.Customers.AsNoTracking(), User);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.CustomerName.Contains(search) || c.Email.Contains(search) || c.Phone.Contains(search) || c.CompanyName.Contains(search));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        const int pageSize = 20;
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.PageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return View(await query.OrderBy(c => c.CustomerName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var customer = await _access.Customers(_context.Customers.AsNoTracking(), User)
            .Include(c => c.Activities).Include(c => c.FollowUps).Include(c => c.Opportunities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        return customer == null ? NotFound() : View(customer);
    }

    public async Task<IActionResult> Create()
    {
        await LoadOwners();
        return View(new Customer());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (_access.CanSeeAll(User))
        {
            if (!await _access.IsValidOwnerAsync(customer.OwnerId))
                ModelState.AddModelError(nameof(customer.OwnerId), "Assign the customer to an active Sales Executive.");
        }
        else customer.OwnerId = _access.UserId(User);

        if (await _context.Customers.AnyAsync(c => c.Email == customer.Email || c.Phone == customer.Phone))
            ModelState.AddModelError("", "A customer with this email or phone already exists.");
        if (!ModelState.IsValid)
        {
            await LoadOwners(customer.OwnerId);
            return View(customer);
        }

        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = _access.UserId(User);
        customer.CustomerCode = $"CUS-{DateTime.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Create", "Customer", customer.CustomerId, $"Created customer: {customer.CustomerName}");
        TempData["Success"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var customer = await _access.Customers(_context.Customers, User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null) return NotFound();
        await LoadOwners(customer.OwnerId);
        return View(customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId) return NotFound();
        var existing = await _access.Customers(_context.Customers, User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (existing == null) return NotFound();
        if (!_access.CanSeeAll(User) && existing.OwnerId != _access.UserId(User)) return Forbid();
        if (_access.CanSeeAll(User) && !await _access.IsValidOwnerAsync(customer.OwnerId))
            ModelState.AddModelError(nameof(customer.OwnerId), "Assign the customer to an active Sales Executive.");
        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && (c.Email == customer.Email || c.Phone == customer.Phone)))
            ModelState.AddModelError("", "Another customer already uses this email or phone.");
        if (!ModelState.IsValid)
        {
            customer.OwnerId = _access.CanSeeAll(User) ? customer.OwnerId : existing.OwnerId;
            await LoadOwners(customer.OwnerId);
            return View(customer);
        }

        var old = $"{existing.CustomerName}; {existing.Email}; {existing.Phone}; {existing.Status}";
        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        existing.ModifiedDate = DateTime.UtcNow;
        if (_access.CanSeeAll(User)) existing.OwnerId = customer.OwnerId;
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", "Customer", id, $"Updated customer: {existing.CustomerName}", oldValue: old, newValue: $"{existing.CustomerName}; {existing.Email}; {existing.Phone}; {existing.Status}");
        TempData["Success"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var customer = await _access.Customers(_context.Customers.AsNoTracking(), User)
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        return customer == null ? NotFound() : View(customer);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _access.Customers(_context.Customers, User).FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null) return NotFound();
        var hasHistory = await _context.Opportunities.AnyAsync(x => x.CustomerId == id)
            || await _context.FollowUps.AnyAsync(x => x.CustomerId == id)
            || await _context.Activities.AnyAsync(x => x.CustomerId == id);
        if (hasHistory)
        {
            customer.Status = "Inactive";
            customer.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Deactivate", "Customer", id, "Customer deactivated to preserve linked history.");
            TempData["Success"] = "Customer has related history and was deactivated instead of deleted.";
            return RedirectToAction(nameof(Index));
        }
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Delete", "Customer", id, $"Deleted customer: {customer.CustomerName}");
        TempData["Success"] = "Customer deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOwners(string? selected = null)
    {
        ViewBag.Owners = new SelectList(await _access.SalesUsersAsync(), "Id", "DisplayName", selected);
    }
}
