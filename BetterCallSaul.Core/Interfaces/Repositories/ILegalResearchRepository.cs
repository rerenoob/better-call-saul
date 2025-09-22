using BetterCallSaul.Core.Models.NoSQL;

namespace BetterCallSaul.Core.Interfaces.Repositories;

public interface ILegalResearchRepository
{
    Task<List<LegalResearchDocument>> SearchTextAsync(string query, int limit = 50);
    Task<List<LegalResearchDocument>> FindSimilarCasesAsync(string caseText, double threshold = 0.7);
    Task<LegalResearchDocument?> GetByCitationAsync(string citation);
    Task<LegalResearchDocument?> GetByIdAsync(string id);
    Task<LegalResearchDocument> CreateAsync(LegalResearchDocument document);
    Task<LegalResearchDocument> UpdateAsync(LegalResearchDocument document);
    Task DeleteAsync(string id);
    Task BulkIndexAsync(List<LegalResearchDocument> documents);
    Task<List<LegalResearchDocument>> GetByJurisdictionAsync(string jurisdiction, int limit = 100);
    Task<List<LegalResearchDocument>> GetByCourtAsync(string court, int limit = 100);
    Task<List<LegalResearchDocument>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int limit = 100);
    Task<List<LegalResearchDocument>> SearchAdvancedAsync(LegalSearchQuery query);
    Task<long> CountAsync();
    Task<Dictionary<string, long>> GetStatsAsync();
}

public class LegalSearchQuery
{
    public string? FullTextQuery { get; set; }
    public List<string>? Keywords { get; set; }
    public string? Jurisdiction { get; set; }
    public string? Court { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public LegalDocumentType? DocumentType { get; set; }
    public List<string>? Topics { get; set; }
    public List<string>? PracticeAreas { get; set; }
    public double? MinRelevanceScore { get; set; }
    public PrecedentialValue? PrecedentialValue { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public string? SortBy { get; set; } = "relevanceScore";
    public bool SortDescending { get; set; } = true;
}