namespace AcxiomCRM.ViewModels;

public class ReportViewModel
{
    public string Report { get; set; } = "customers";
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Owner { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageCount { get; set; } = 1;
    public int TotalCount { get; set; }
    public List<ReportRow> Rows { get; set; } = new();
}

public class ReportRow
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Related { get; set; } = "";
    public decimal? Amount { get; set; }
    public DateTime? Date { get; set; }
    public string Details { get; set; } = "";
}
