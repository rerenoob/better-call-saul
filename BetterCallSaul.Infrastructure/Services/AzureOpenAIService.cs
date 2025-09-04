using Azure;
using Azure.AI.OpenAI;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models;
using BetterCallSaul.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BetterCallSaul.Infrastructure.Services;

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly OpenAIClient? _openAIClient;
    private readonly OpenAIOptions _options;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(IOptions<OpenAIOptions> options, ILogger<AzureOpenAIService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogWarning("Azure OpenAI configuration is missing. Service will be disabled.");
            return;
        }

        _openAIClient = new OpenAIClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey)
        );
    }

    public async Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (_openAIClient == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "Azure OpenAI service is not configured",
                ProcessingTime = TimeSpan.Zero
            };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _options.DeploymentName,
                MaxTokens = request.MaxTokens,
                Temperature = (float?)request.Temperature,
                Messages =
                {
                    new ChatRequestSystemMessage("You are an AI legal assistant specializing in case analysis for public defenders. Provide thorough, objective analysis with confidence scores."),
                    new ChatRequestUserMessage(BuildCaseAnalysisPrompt(request.DocumentText, request.CaseContext))
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            var completion = response.Value;
            
            return new AIResponse
            {
                Success = true,
                GeneratedText = completion.Choices[0].Message.Content,
                TokensUsed = completion.Usage.TotalTokens,
                ProcessingTime = DateTime.UtcNow - startTime,
                Model = _options.Model,
                ConfidenceScore = 0.85 // Placeholder, would extract from response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing case with Azure OpenAI");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTime = TimeSpan.Zero
            };
        }
    }

    public async Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default)
    {
        var request = new AIRequest
        {
            DocumentText = documentText,
            CaseContext = caseContext,
            MaxTokens = _options.MaxTokens,
            Temperature = _options.Temperature
        };
        
        return await AnalyzeCaseAsync(request, cancellationToken);
    }

    public async Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default)
    {
        if (_openAIClient == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "Azure OpenAI service is not configured",
                ProcessingTime = TimeSpan.Zero
            };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _options.DeploymentName,
                MaxTokens = 1000,
                Temperature = 0.2f,
                Messages =
                {
                    new ChatRequestSystemMessage("You are an AI legal predictor specializing in case outcome prediction for public defenders. Provide probability scores and key factors."),
                    new ChatRequestUserMessage(BuildPredictionPrompt(caseDetails))
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            var completion = response.Value;
            
            return new AIResponse
            {
                Success = true,
                GeneratedText = completion.Choices[0].Message.Content,
                TokensUsed = completion.Usage.TotalTokens,
                ProcessingTime = DateTime.UtcNow - startTime,
                Model = _options.Model,
                ConfidenceScore = 0.8 // Placeholder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting case outcome with Azure OpenAI");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTime = TimeSpan.Zero
            };
        }
    }

    public async Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default)
    {
        if (_openAIClient == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "Azure OpenAI service is not configured",
                ProcessingTime = TimeSpan.Zero
            };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _options.DeploymentName,
                MaxTokens = 800,
                Temperature = 0.1f,
                Messages =
                {
                    new ChatRequestSystemMessage("You are an AI legal document summarizer. Create concise, accurate summaries highlighting key legal points and facts."),
                    new ChatRequestUserMessage(BuildSummaryPrompt(documentText))
                }
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);
            var completion = response.Value;
            
            return new AIResponse
            {
                Success = true,
                GeneratedText = completion.Choices[0].Message.Content,
                TokensUsed = completion.Usage.TotalTokens,
                ProcessingTime = DateTime.UtcNow - startTime,
                Model = _options.Model,
                ConfidenceScore = 0.9 // Placeholder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing legal document with Azure OpenAI");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTime = TimeSpan.Zero
            };
        }
    }

    public async IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_openAIClient == null)
        {
            yield break;
        }

        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = _options.DeploymentName,
            MaxTokens = request.MaxTokens,
            Temperature = (float?)request.Temperature,
            Messages =
            {
                new ChatRequestSystemMessage("You are an AI legal assistant. Provide real-time analysis as it becomes available."),
                new ChatRequestUserMessage(BuildCaseAnalysisPrompt(request.DocumentText, request.CaseContext))
            }
        };

        var response = await _openAIClient.GetChatCompletionsStreamingAsync(chatCompletionsOptions, cancellationToken);
        
        await foreach (var update in response)
        {
            if (update.ContentUpdate != null)
            {
                yield return update.ContentUpdate;
            }
        }
    }

    private string BuildCaseAnalysisPrompt(string? documentText, string? caseContext)
    {
        return $"""
        Analyze the following legal document and provide a comprehensive case analysis:
        
        DOCUMENT TEXT:
        {documentText}
        
        CASE CONTEXT:
        {caseContext}
        
        Please provide analysis in the following format:
        1. Case Viability Assessment (0-100%)
        2. Key Legal Issues Identified
        3. Potential Defenses and Arguments
        4. Evidence Strength Evaluation
        5. Timeline and Chronological Analysis
        6. Recommended Next Steps
        
        Be objective, thorough, and include confidence scores where appropriate.
        """;
    }

    private string BuildPredictionPrompt(string caseDetails)
    {
        return $"""
        Predict the likely outcome for the following case details:
        
        {caseDetails}
        
        Provide:
        1. Success Probability (0-100%) with confidence interval
        2. Key Factors Influencing Prediction
        3. Risk Assessment
        4. Historical Comparison with Similar Cases
        5. Strategic Recommendations
        
        Format the response clearly for legal professionals.
        """;
    }

    private string BuildSummaryPrompt(string documentText)
    {
        return $"""
        Summarize the following legal document concisely:
        
        {documentText}
        
        Focus on:
        - Key facts and events
        - Legal issues and arguments
        - Important dates and timelines
        - Critical evidence mentioned
        - Potential legal implications
        
        Keep the summary under 300 words and use clear, professional language.
        """;
    }
}