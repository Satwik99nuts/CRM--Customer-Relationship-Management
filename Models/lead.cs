
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Lead
{
    public int LeadId { get; set; }

    [StringLength(20)]
    public string LeadCode { get; set; } = "";

    [Required, StringLength(100)]
    public string LeadName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = "";

    [StringLength(150)]
    public string CompanyName { get; set; } = "";
    [Required, StringLength(40)]
    public string Source { get; set; } = "Website";
    [Required, RegularExpression("^(New|Contacted|Qualified|Unqualified|Converted|Lost)$", ErrorMessage = "Select a valid lead status.")]
    public string Status { get; set; } = "New";
    [Range(0, 1000000000)]
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string OwnerId { get; set; } = "";
    public int? ConvertedCustomerId { get; set; }
    public DateTime? ConvertedDate { get; set; }

    public Customer? ConvertedCustomer { get; set; }
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
