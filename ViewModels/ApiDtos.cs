using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Company { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}

public class CustomerInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(20)] public string Phone { get; set; } = "";
    [StringLength(150)] public string Company { get; set; } = "";
    [StringLength(250)] public string Address { get; set; } = "";
    [StringLength(80)] public string City { get; set; } = "";
    [StringLength(80)] public string State { get; set; } = "";
    [Required, RegularExpression("^(Active|Inactive)$")] public string Status { get; set; } = "Active";
}

public class LeadDto
{
    public int Id { get; set; }
    public string LeadCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Company { get; set; } = "";
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal ExpectedValue { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LeadInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(20)] public string Phone { get; set; } = "";
    [StringLength(150)] public string Company { get; set; } = "";
    [Required, StringLength(40)] public string Source { get; set; } = "Website";
    [Required, RegularExpression("^(New|Contacted|Qualified|Unqualified|Converted|Lost)$")] public string Status { get; set; } = "New";
    [Range(0, 1000000000)] public decimal ExpectedValue { get; set; }
}

public class OpportunityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public int Probability { get; set; }
    public string Stage { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime ExpectedCloseDate { get; set; }
    public decimal WeightedAmount => Amount * Probability / 100m;
}

public class OpportunityInput
{
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required] public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    [Range(0.01, 1000000000)] public decimal Amount { get; set; }
    [Range(0, 100)] public int Probability { get; set; } = 10;
    [Required, RegularExpression("^(Qualification|Proposal|Negotiation|Won|Lost)$")] public string Stage { get; set; } = "Qualification";
    [Required, RegularExpression("^(Open|Won|Lost)$")] public string Status { get; set; } = "Open";
    [DataType(DataType.Date)] public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
}

public class FollowUpDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = "";
    public DateTime FollowUpDate { get; set; }
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
    public string Remarks { get; set; } = "";
}

public class FollowUpInput
{
    [Required, StringLength(200)] public string Subject { get; set; } = "";
    [DataType(DataType.Date)] public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1);
    [Required, StringLength(30)] public string Type { get; set; } = "Call";
    [Required, StringLength(20)] public string Status { get; set; } = "Planned";
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }
    [StringLength(1000)] public string Remarks { get; set; } = "";
}

public class ApiLoginInput
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
