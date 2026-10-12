
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Lead
{
    public int LeadId { get; set; }

    [Required, StringLength(100)]
    public string LeadName { get; set; } = "";

    [EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Phone, StringLength(20)]
    public string Phone { get; set; } = "";

    public string CompanyName { get; set; } = "";
    public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
