using BetterCallSaul.Core.Models.Entities;

namespace BetterCallSaul.Core.Interfaces.Services;

public interface ISuccessPredictionService
{
    Task<SuccessPrediction> PredictCaseOutcomeAsync(Guid caseId, Guid analysisId, string caseAnalysis, CancellationToken cancellationToken = default);
    Task<SuccessPrediction> GetPredictionAsync(Guid predictionId, CancellationToken cancellationToken = default);
    Task<List<SuccessPrediction>> GetCasePredictionsAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task UpdatePredictionAccuracyAsync(Guid predictionId, double actualSuccess, CancellationToken cancellationToken = default);
    
    // Model performance tracking
    Task<ModelPerformance> GetModelPerformanceAsync(CancellationToken cancellationToken = default);
    Task<List<PredictionAccuracy>> GetPredictionHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}

public class ModelPerformance
{
    public double OverallAccuracy { get; set; } // 0-1
    public double Precision { get; set; } // 0-1
    public double Recall { get; set; } // 0-1
    public double F1Score { get; set; } // 0-1
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public Dictionary<string, double> AccuracyByCaseType { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class PredictionAccuracy
{
    public Guid PredictionId { get; set; }
    public Guid CaseId { get; set; }
    public double PredictedSuccess { get; set; } // 0-100%
    public double ActualSuccess { get; set; } // 0-100%
    public double Error { get; set; } // Absolute difference
    public double AbsoluteError { get; set; } // |predicted - actual|
    public DateTime PredictionDate { get; set; }
    public DateTime? OutcomeDate { get; set; }
}