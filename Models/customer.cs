
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Customer
{
    public int CustomerId { get; set; }

    [StringLength(20)]
    public string CustomerCode { get; set; } = "";

    [Required, StringLength(100)]
    public string CustomerName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = "";

    [StringLength(150)]
    public string CompanyName { get; set; } = "";

    [StringLength(250)]
    public string Address { get; set; } = "";

    [StringLength(80)]
    public string City { get; set; } = "";

    [StringLength(80)]
    public string State { get; set; } = "";

    [Required, RegularExpression("^(Active|Inactive)$", ErrorMessage = "Select a valid customer status.")]
    public string Status { get; set; } = "Active";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public string CreatedBy { get; set; } = "";
    public string OwnerId { get; set; } = "";

    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
