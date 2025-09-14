using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using BetterCallSaul.Core.Configuration;
using BetterCallSaul.Core.Models.Entities;
using BetterCallSaul.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BetterCallSaul.Infrastructure.Services.AI;

public class AWSBedrockService : IAIService
{
    private readonly AmazonBedrockRuntimeClient? _bedrockClient;
    private readonly BedrockOptions _options;
    private readonly ILogger<AWSBedrockService> _logger;

    public AWSBedrockService(IOptions<AWSOptions> awsOptions, ILogger<AWSBedrockService> logger)
    {
        _options = awsOptions.Value.Bedrock;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.Region) || string.IsNullOrEmpty(_options.ModelId))
        {
            _logger.LogWarning("AWS Bedrock configuration is missing. Service will be disabled.");
            return;
        }

        try
        {
            var config = new AmazonBedrockRuntimeConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region)
            };
            
            _bedrockClient = new AmazonBedrockRuntimeClient(config);
            _logger.LogInformation("AWS Bedrock service initialized with model: {ModelId}", _options.ModelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AWS Bedrock client. Exception type: {ExceptionType}", ex.GetType().Name);
        }
    }

    public async Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (_bedrockClient == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "AWS Bedrock service is not configured",
                ProcessingTime = TimeSpan.Zero
            };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            var bedrockRequest = CreateBedrockRequest(request);
            var response = await _bedrockClient.InvokeModelAsync(bedrockRequest, cancellationToken);
            
            var result = ProcessBedrockResponse(response);
            result.ProcessingTime = DateTime.UtcNow - startTime;
            result.Model = _options.ModelId;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing case with AWS Bedrock. Exception type: {ExceptionType}", ex.GetType().Name);
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"AWS Bedrock service error: {ex.Message}",
                ProcessingTime = TimeSpan.Zero,
                Metadata = new Dictionary<string, object>
                {
                    ["error_type"] = ex.GetType().Name,
                    ["aws_bedrock_failed"] = true
                }
            };
        }
    }

    public async Task<AIResponse> GenerateLegalAnalysisAsync(string documentText, string caseContext, CancellationToken cancellationToken = default)
    {
        var request = new AIRequest
        {
            DocumentText = documentText,
            CaseContext = caseContext,
            MaxTokens = 2000,
            Temperature = 0.3
        };
        
        return await AnalyzeCaseAsync(request, cancellationToken);
    }

    public async Task<AIResponse> PredictCaseOutcomeAsync(string caseDetails, CancellationToken cancellationToken = default)
    {
        if (_bedrockClient == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "AWS Bedrock service is not configured",
                ProcessingTime = TimeSpan.Zero
            };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            var request = new AIRequest
            {
                Prompt = BuildPredictionPrompt(caseDetails),
                MaxTokens = 1000,
                Temperature = 0.2
            };
            
            var bedrockRequest = CreateBedrockRequest(request);
            var response = await _bedrockClient.InvokeModelAsync(bedrockRequest, cancellationToken);
            
            var result = ProcessBedrockResponse(response);
            result.ProcessingTime = DateTime.UtcNow - startTime;
            result.Model = _options.ModelId;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting case outcome with AWS Bedrock. Exception type: {ExceptionType}", ex.GetType().Name);
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"AWS Bedrock prediction error: {ex.Message}",
                ProcessingTime = TimeSpan.Zero,
                Metadata = new Dictionary<string, object>
                {
                    ["error_type"] = ex.GetType().Name,
                    ["aws_bedrock_failed"] = true
                }
            };
        }
    }

    public async Task<AIResponse> SummarizeLegalDocumentAsync(string documentText, CancellationToken cancellationToken = default)
    {
        if (_bedrockClient == null)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "AWS Bedrock service is not configured",
                ProcessingTime = TimeSpan.Zero
            };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            var request = new AIRequest
            {
                Prompt = BuildSummaryPrompt(documentText),
                MaxTokens = 800,
                Temperature = 0.1
            };
            
            var bedrockRequest = CreateBedrockRequest(request);
            var response = await _bedrockClient.InvokeModelAsync(bedrockRequest, cancellationToken);
            
            var result = ProcessBedrockResponse(response);
            result.ProcessingTime = DateTime.UtcNow - startTime;
            result.Model = _options.ModelId;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing legal document with AWS Bedrock. Exception type: {ExceptionType}", ex.GetType().Name);
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"AWS Bedrock summarization error: {ex.Message}",
                ProcessingTime = TimeSpan.Zero,
                Metadata = new Dictionary<string, object>
                {
                    ["error_type"] = ex.GetType().Name,
                    ["aws_bedrock_failed"] = true
                }
            };
        }
    }

    public async IAsyncEnumerable<string> StreamAnalysisAsync(AIRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_bedrockClient == null || string.IsNullOrEmpty(_options.ModelId))
        {
            yield break;
        }

        InvokeModelWithResponseStreamResponse? response = null;
        
        try
        {
            var bedrockRequest = CreateBedrockRequest(request);
            var streamRequest = new InvokeModelWithResponseStreamRequest
            {
                ModelId = _options.ModelId,
                Body = bedrockRequest.Body
            };

            response = await _bedrockClient.InvokeModelWithResponseStreamAsync(streamRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AWS Bedrock streaming analysis setup");
            yield break;
        }

        if (response?.Body != null)
        {
            foreach (var chunk in response.Body)
            {
                if (chunk is PayloadPart payloadPart)
                {
                    var chunkText = ProcessStreamChunk(payloadPart);
                    if (!string.IsNullOrEmpty(chunkText))
                    {
                        yield return chunkText;
                    }
                }
            }
        }
    }

    private InvokeModelRequest CreateBedrockRequest(AIRequest request)
    {
        var prompt = !string.IsNullOrEmpty(request.Prompt) 
            ? request.Prompt 
            : BuildCaseAnalysisPrompt(request.DocumentText, request.CaseContext);

        var bedrockPayload = new
        {
            prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
            max_tokens_to_sample = request.MaxTokens,
            temperature = request.Temperature,
            stop_sequences = new[] { "\n\nHuman:" }
        };

        return new InvokeModelRequest
        {
            ModelId = _options.ModelId,
            ContentType = "application/json",
            Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockPayload)))
        };
    }

    private AIResponse ProcessBedrockResponse(InvokeModelResponse response)
    {
        using var streamReader = new System.IO.StreamReader(response.Body);
        var responseText = streamReader.ReadToEnd();
        
        try
        {
            var bedrockResponse = JsonSerializer.Deserialize<BedrockCompletionResponse>(responseText);
            
            return new AIResponse
            {
                Success = true,
                GeneratedText = bedrockResponse?.Completion?.Trim(),
                TokensUsed = bedrockResponse?.Completion?.Length / 4 ?? 0, // Rough estimate
                ConfidenceScore = 0.85
            };
        }
        catch (JsonException)
        {
            // Fallback: return raw response if JSON parsing fails
            return new AIResponse
            {
                Success = true,
                GeneratedText = responseText.Trim(),
                TokensUsed = responseText.Length / 4,
                ConfidenceScore = 0.8
            };
        }
    }

    private string ProcessStreamChunk(PayloadPart chunk)
    {
        try
        {
            var chunkBytes = chunk.Bytes.ToArray();
            var chunkText = System.Text.Encoding.UTF8.GetString(chunkBytes);
            
            var completionResponse = JsonSerializer.Deserialize<BedrockCompletionResponse>(chunkText);
            return completionResponse?.Completion ?? string.Empty;
        }
        catch
        {
            return string.Empty;
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

    private class BedrockCompletionResponse
    {
        public string? Completion { get; set; }
        public string? StopReason { get; set; }
    }
}