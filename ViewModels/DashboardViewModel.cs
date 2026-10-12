using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public int CompletedActivities { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal WeightedPipeline { get; set; }
    public List<StageSummary> Stages { get; set; } = new();
    public List<LeadStatusSummary> LeadStatuses { get; set; } = new();
    public List<MonthlySalesSummary> MonthlySales { get; set; } = new();
    public List<Activity> RecentActivities { get; set; } = new();
}

public class StageSummary
{
    public string Stage { get; set; } = "";
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class LeadStatusSummary
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class MonthlySalesSummary
{
    public string Month { get; set; } = "";
    public decimal Amount { get; set; }
}
