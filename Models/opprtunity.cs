
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Opportunity
{
    public int OpportunityId { get; set; }

    [Required, StringLength(150)]
    public string OpportunityName { get; set; } = "";

    [Range(0.01, 1000000000)]
    public decimal Amount { get; set; }

    [Range(0, 100)]
    public int Probability { get; set; } = 50;

    [Required, StringLength(30)]
    public string Stage { get; set; } = "Qualification";
    [Required, StringLength(20)]
    public string Status { get; set; } = "Open";
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string OwnerId { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
