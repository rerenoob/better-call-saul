using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.ML;

public class LegalTextSimilarity
{
    private readonly ILogger<LegalTextSimilarity> _logger;

    public LegalTextSimilarity(ILogger<LegalTextSimilarity> logger)
    {
        _logger = logger;
    }

    public decimal CalculateCosineSimilarity(string text1, string text2)
    {
        try
        {
            var vector1 = GetTextVector(NormalizeLegalText(text1));
            var vector2 = GetTextVector(NormalizeLegalText(text2));

            if (vector1.Count == 0 || vector2.Count == 0)
                return 0m;

            var dotProduct = CalculateDotProduct(vector1, vector2);
            var magnitude1 = CalculateMagnitude(vector1);
            var magnitude2 = CalculateMagnitude(vector2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0m;

            return (decimal)(dotProduct / (magnitude1 * magnitude2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cosine similarity");
            return 0m;
        }
    }

    public decimal CalculateJaccardSimilarity(string text1, string text2)
    {
        try
        {
            var set1 = new HashSet<string>(TokenizeText(NormalizeLegalText(text1)));
            var set2 = new HashSet<string>(TokenizeText(NormalizeLegalText(text2)));

            if (set1.Count == 0 && set2.Count == 0)
                return 1m;

            if (set1.Count == 0 || set2.Count == 0)
                return 0m;

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return (decimal)intersection / union;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Jaccard similarity");
            return 0m;
        }
    }

    public decimal CalculateWeightedSimilarity(string text1, string text2, Dictionary<string, decimal> weights)
    {
        try
        {
            var normalized1 = NormalizeLegalText(text1);
            var normalized2 = NormalizeLegalText(text2);

            var totalWeight = weights.Values.Sum();
            if (totalWeight == 0)
                return 0m;

            decimal weightedSum = 0m;

            // Legal citation similarity
            if (weights.TryGetValue("citations", out decimal citationWeight) && citationWeight > 0)
            {
                var citations1 = ExtractCitations(normalized1);
                var citations2 = ExtractCitations(normalized2);
                var citationSimilarity = CalculateCitationSimilarity(citations1, citations2);
                weightedSum += citationSimilarity * citationWeight;
            }

            // Legal terminology similarity
            if (weights.TryGetValue("terminology", out decimal terminologyWeight) && terminologyWeight > 0)
            {
                var terminologySimilarity = CalculateTerminologySimilarity(normalized1, normalized2);
                weightedSum += terminologySimilarity * terminologyWeight;
            }

            // Semantic similarity
            if (weights.TryGetValue("semantic", out decimal semanticWeight) && semanticWeight > 0)
            {
                var semanticSimilarity = CalculateCosineSimilarity(normalized1, normalized2);
                weightedSum += semanticSimilarity * semanticWeight;
            }

            return weightedSum / totalWeight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating weighted similarity");
            return 0m;
        }
    }

    private Dictionary<string, double> GetTextVector(string text)
    {
        var tokens = TokenizeText(text);
        var vector = new Dictionary<string, double>();

        foreach (var token in tokens)
        {
            if (vector.ContainsKey(token))
                vector[token]++;
            else
                vector[token] = 1;
        }

        return vector;
    }

    private double CalculateDotProduct(Dictionary<string, double> vector1, Dictionary<string, double> vector2)
    {
        double dotProduct = 0;

        foreach (var key in vector1.Keys)
        {
            if (vector2.ContainsKey(key))
            {
                dotProduct += vector1[key] * vector2[key];
            }
        }

        return dotProduct;
    }

    private double CalculateMagnitude(Dictionary<string, double> vector)
    {
        double sum = 0;
        foreach (var value in vector.Values)
        {
            sum += value * value;
        }
        return Math.Sqrt(sum);
    }

    private string NormalizeLegalText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove excessive whitespace
        text = Regex.Replace(text, @"\s+", " ");

        // Remove common legal boilerplate
        text = Regex.Replace(text, @"\b(court|judge|justice|plaintiff|defendant|appellant|appellee)\b", "");

        // Remove punctuation except for legal citations
        text = Regex.Replace(text, @"[^\w\s§]", " ");

        return text.Trim();
    }

    private IEnumerable<string> TokenizeText(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Where(word => word.Length > 2) // Filter out very short words
                   .Distinct();
    }

    private List<string> ExtractCitations(string text)
    {
        var citations = new List<string>();
        
        // Match common legal citation patterns
        var patterns = new[]
        {
            @"\b\d+\s+U\.?S\.?\s+\d+\b",          // US Supreme Court
            @"\b\d+\s+F\.?\d*d\?\s+\d+\b",        // Federal cases
            @"\b\d+\s+S\.?\s+\d+\b",              // State cases
            @"\b\d+\s+\w+\.?\s+\d+\b",           // General case pattern
            @"\b\w+\s+§\s*\d+\.?\d*\b",          // Statute sections
            @"\b\d+\s+CFR\s+§\s*\d+\.?\d*\b"     // CFR regulations
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern);
            citations.AddRange(matches.Select(m => m.Value));
        }

        return citations.Distinct().ToList();
    }

    private decimal CalculateCitationSimilarity(List<string> citations1, List<string> citations2)
    {
        if (citations1.Count == 0 && citations2.Count == 0)
            return 1m;

        if (citations1.Count == 0 || citations2.Count == 0)
            return 0m;

        var intersection = citations1.Intersect(citations2).Count();
        var union = citations1.Union(citations2).Count();

        return (decimal)intersection / union;
    }

    private decimal CalculateTerminologySimilarity(string text1, string text2)
    {
        // Common legal terminology for weighted matching
        var legalTerms = new HashSet<string>
        {
            "jurisdiction", "precedent", "statute", "regulation", "constitution",
            "liability", "negligence", "contract", "tort", "damages", "injunction",
            "writ", "mandamus", "habeas", "corpus", "appeal", "affirm", "reverse",
            "remand", "motion", "pleading", "discovery", "evidence", "testimony",
            "witness", "exhibit", "objection", "sustain", "overrule", "verdict",
            "judgment", "sentence", "probation", "parole", "bail", "arraignment",
            "indictment", "information", "complaint", "petition", "brief", "opinion",
            "holding", "dicta", "ratio", "obiter", "stare", "decisis"
        };

        var tokens1 = TokenizeText(text1).Where(legalTerms.Contains);
        var tokens2 = TokenizeText(text2).Where(legalTerms.Contains);

        var set1 = new HashSet<string>(tokens1);
        var set2 = new HashSet<string>(tokens2);

        if (set1.Count == 0 && set2.Count == 0)
            return 1m;

        if (set1.Count == 0 || set2.Count == 0)
            return 0m;

        var intersection = set1.Intersect(set2).Count();
        var union = set1.Union(set2).Count();

        return (decimal)intersection / union;
    }

    public decimal CalculateOverallSimilarity(string text1, string text2)
    {
        var weights = new Dictionary<string, decimal>
        {
            ["citations"] = 0.3m,
            ["terminology"] = 0.4m,
            ["semantic"] = 0.3m
        };

        return CalculateWeightedSimilarity(text1, text2, weights);
    }
}