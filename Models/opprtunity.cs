
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

    public string Stage { get; set; } = "Qualification";
    public string Status { get; set; } = "Open";
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
