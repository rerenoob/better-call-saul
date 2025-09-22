using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BetterCallSaul.Core.Models.NoSQL;

namespace BetterCallSaul.Infrastructure.Data.NoSQL;

public class NoSqlContext
{
    private readonly IMongoDatabase _database;
    
    public NoSqlContext(IMongoClient mongoClient, IOptions<NoSqlSettings> settings)
    {
        _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
    }
    
    public IMongoCollection<CaseDocument> CaseDocuments => 
        _database.GetCollection<CaseDocument>("caseDocuments");
    
    public IMongoCollection<LegalResearchDocument> LegalResearchDocuments => 
        _database.GetCollection<LegalResearchDocument>("legalResearchDocuments");
    
    public IMongoCollection<CaseMatchDocument> CaseMatches => 
        _database.GetCollection<CaseMatchDocument>("caseMatches");
}

public class NoSqlSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "BetterCallSaulNoSQL";
}