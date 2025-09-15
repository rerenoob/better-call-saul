using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BetterCallSaul.CaseService.Models.Entities;

namespace BetterCallSaul.CaseService.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<CaseDocument> Cases => _database.GetCollection<CaseDocument>("cases");
    public IMongoCollection<DocumentDocument> Documents => _database.GetCollection<DocumentDocument>("documents");
    public IMongoCollection<CaseAnalysisDocument> CaseAnalyses => _database.GetCollection<CaseAnalysisDocument>("case_analyses");
    public IMongoCollection<LegalResearchDocument> LegalResearch => _database.GetCollection<LegalResearchDocument>("legal_research");
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}