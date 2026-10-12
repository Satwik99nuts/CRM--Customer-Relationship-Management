using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AcxiomCRM.Services;

public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        int? recordId = null,
        string details = "",
        string result = "Success")
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? "System";

        var log = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            UserId = userId,
            Details = string.IsNullOrWhiteSpace(result)
                ? details
                : $"{details} | Result: {result}",
            CreatedDate = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

}