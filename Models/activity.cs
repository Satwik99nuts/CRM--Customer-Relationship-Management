
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Activity
{
    public int ActivityId { get; set; }

    [Required, StringLength(150)]
    public string Subject { get; set; } = "";

    [Required, StringLength(30)]
    public string ActivityType { get; set; } = "Call";
    [StringLength(1000)]
    public string Description { get; set; } = "";
    [DataType(DataType.Date)]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;
    [Required, StringLength(20)]
    public string Status { get; set; } = "Completed";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string AssignedTo { get; set; } = "";
}
