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
        string result = "Success",
        string oldValue = "",
        string newValue = "",
        string? userIdOverride = null)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var userId = userIdOverride ?? user?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? "System";

        var log = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            UserId = userId,
            Details = details,
            Result = result,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "",
            CreatedDate = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

}
