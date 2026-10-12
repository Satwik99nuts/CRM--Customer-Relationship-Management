
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = "";

    [Required, StringLength(100)]
    public string EntityName { get; set; } = "";

    public int? RecordId { get; set; }
    public string UserId { get; set; } = "System";
    public string Details { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
