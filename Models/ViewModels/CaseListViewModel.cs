using better_call_saul.Models;

namespace better_call_saul.Models.ViewModels;

public class CaseListViewModel
{
    public IEnumerable<CaseSummary> Cases { get; set; } = new List<CaseSummary>();
    public int TotalCases { get; set; }
    public Dictionary<CaseStatus, int> StatusCounts { get; set; } = new();
}

public class CaseSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DocumentCount { get; set; }
}