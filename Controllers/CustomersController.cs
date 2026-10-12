using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public CustomersController(
        ApplicationDbContext context,
        AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.CustomerName.Contains(search) ||
                c.Email.Contains(search) ||
                c.Phone.Contains(search) ||
                c.CompanyName.Contains(search));
        }

        ViewBag.Search = search;

        return View(await query
            .OrderBy(c => c.CustomerName)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        return customer == null ? NotFound() : View(customer);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (await _context.Customers.AnyAsync(c =>
            c.Email == customer.Email ||
            c.Phone == customer.Phone))
        {
            ModelState.AddModelError(
                "",
                "A customer with this email or phone already exists.");
        }

        if (!ModelState.IsValid)
            return View(customer);

        customer.CreatedDate = DateTime.UtcNow;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Create",
            "Customer",
            customer.CustomerId,
            $"Created customer: {customer.CustomerName}");

        TempData["Success"] = "Customer created successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var customer = await _context.Customers.FindAsync(id);

        return customer == null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId)
            return NotFound();

        if (await _context.Customers.AnyAsync(c =>
            c.CustomerId != id &&
            (c.Email == customer.Email || c.Phone == customer.Phone)))
        {
            ModelState.AddModelError(
                "",
                "Another customer already uses this email or phone.");
        }

        if (!ModelState.IsValid)
            return View(customer);

        var existing = await _context.Customers.FindAsync(id);

        if (existing == null)
            return NotFound();

        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.Status = customer.Status;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Update",
            "Customer",
            existing.CustomerId,
            $"Updated customer: {existing.CustomerName}");

        TempData["Success"] = "Customer updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        return customer == null ? NotFound() : View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return NotFound();

        var customerName = customer.CustomerName;
        var customerId = customer.CustomerId;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "Delete",
            "Customer",
            customerId,
            $"Deleted customer: {customerName}");

        TempData["Success"] = "Customer deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}