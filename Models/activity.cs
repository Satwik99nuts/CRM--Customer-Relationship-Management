
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Activity
{
    public int ActivityId { get; set; }

    [Required, StringLength(150)]
    public string Subject { get; set; } = "";

    public string ActivityType { get; set; } = "Call";
    public string Description { get; set; } = "";
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Completed";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
}
