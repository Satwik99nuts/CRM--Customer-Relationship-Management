
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class FollowUp
{
    public int FollowUpId { get; set; }

    [Required, StringLength(200)]
    public string Subject { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);
    [Required, StringLength(30)]
    public string Type { get; set; } = "Call";
    [Required, StringLength(20)]
    public string Status { get; set; } = "Planned";
    [StringLength(1000)]
    public string Remarks { get; set; } = "";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public int? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }
    public string AssignedTo { get; set; } = "";
}
