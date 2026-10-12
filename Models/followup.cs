
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class FollowUp
{
    public int FollowUpId { get; set; }

    [Required, StringLength(200)]
    public string Subject { get; set; } = "";

    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);
    public string Type { get; set; } = "Call";
    public string Status { get; set; } = "Planned";
    public string Remarks { get; set; } = "";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
}
