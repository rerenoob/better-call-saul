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
    private readonly List<string> _fallbackModelIds = new()
    {
        "us.anthropic.claude-3-haiku-20240307-v1:0",
        "us.anthropic.claude-3-sonnet-20240229-v1:0",
        "us.anthropic.claude-3-5-sonnet-20240620-v1:0",
        "us.anthropic.claude-3-opus-20240229-v1:0"
    };

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
            _logger.LogInformation("AWS Bedrock service initialized successfully with model: {ModelId} in region: {Region}",
                _options.ModelId, _options.Region);
            _logger.LogDebug("Fallback models available: {FallbackModels}", string.Join(", ", _fallbackModelIds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AWS Bedrock client. Region: {Region}, Exception type: {ExceptionType}",
                _options.Region, ex.GetType().Name);

            if (ex is Amazon.Runtime.AmazonServiceException awsEx)
            {
                _logger.LogError("AWS Service Exception during initialization - ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                    awsEx.ErrorCode, awsEx.StatusCode);
            }
        }
    }

    public async Task<AIResponse> AnalyzeCaseAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (_bedrockClient == null)
        {
            _logger.LogError("Bedrock client is null - service not configured");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "AWS Bedrock service is not configured",
                ProcessingTime = TimeSpan.Zero
            };
        }

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting Bedrock analysis with primary model: {ModelId}", _options.ModelId);

        // Try primary model first
        var modelsToTry = new List<string> { _options.ModelId };
        modelsToTry.AddRange(_fallbackModelIds.Where(id => id != _options.ModelId));

        foreach (var modelId in modelsToTry)
        {
            try
            {
                _logger.LogInformation("Attempting to use model: {ModelId}", modelId);

                var bedrockRequest = CreateBedrockRequest(request, modelId);
                _logger.LogInformation("Created Bedrock request for model: {ModelId}, ContentType: {ContentType}",
                    modelId, bedrockRequest.ContentType);

                var response = await _bedrockClient.InvokeModelAsync(bedrockRequest, cancellationToken);
                _logger.LogInformation("Received Bedrock response for model: {ModelId}, ContentType: {ContentType}",
                    modelId, response.ContentType);

                var result = ProcessBedrockResponse(response);
                result.ProcessingTime = DateTime.UtcNow - startTime;
                result.Model = modelId;

                _logger.LogInformation("Successfully processed Bedrock response: Success={Success}, TextLength={TextLength}",
                    result.Success, result.GeneratedText?.Length ?? 0);

                if (modelId != _options.ModelId)
                {
                    _logger.LogWarning("Primary model {PrimaryModel} failed, successfully used fallback model {FallbackModel}",
                        _options.ModelId, modelId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model {ModelId} failed with error: {ErrorMessage}. Exception Type: {ExceptionType}",
                    modelId, ex.Message, ex.GetType().Name);

                // Log AWS-specific error details
                if (ex is Amazon.Runtime.AmazonServiceException awsEx)
                {
                    _logger.LogError("AWS Service Exception Details - ErrorCode: {ErrorCode}, StatusCode: {StatusCode}",
                        awsEx.ErrorCode, awsEx.StatusCode);
                }
                else if (ex is Amazon.Runtime.AmazonClientException awsClientEx)
                {
                    _logger.LogWarning("AWS Client Exception: {Message}", awsClientEx.Message);
                }

                if (modelId == modelsToTry.Last())
                {
                    // This was the last model to try
                    _logger.LogError(ex, "All models failed. Last error from {ModelId}: {ErrorMessage}. Region: {Region}",
                        modelId, ex.Message, _options.Region);

                    var errorMessage = "AWS Bedrock service error: All models failed. ";

                    if (ex is Amazon.Runtime.AmazonServiceException awsServiceEx)
                    {
                        if (awsServiceEx.ErrorCode == "AccessDeniedException")
                        {
                            errorMessage += $"Access denied to model {modelId}. Please check:\n" +
                                          "1. AWS Bedrock model access is enabled in AWS Console (Bedrock > Model Access)\n" +
                                          "2. IAM permissions include 'bedrock:InvokeModel' for Anthropic models\n" +
                                          "3. Your AWS account has access to Claude models in the {_options.Region} region";
                        }
                        else if (awsServiceEx.ErrorCode == "ValidationException")
                        {
                            errorMessage += $"Invalid model ID or request format. Error: {awsServiceEx.Message}";
                        }
                        else
                        {
                            errorMessage += $"AWS Error ({awsServiceEx.ErrorCode}): {awsServiceEx.Message}";
                        }
                    }
                    else
                    {
                        errorMessage += $"Last error: {ex.Message}";
                    }

                    return new AIResponse
                    {
                        Success = false,
                        ErrorMessage = errorMessage,
                        ProcessingTime = DateTime.UtcNow - startTime,
                        Metadata = new Dictionary<string, object>
                        {
                            ["error_type"] = ex.GetType().Name,
                            ["aws_bedrock_failed"] = true,
                            ["attempted_models"] = modelsToTry,
                            ["region"] = _options.Region,
                            ["aws_error_code"] = ex is Amazon.Runtime.AmazonServiceException awsSvcEx ? awsSvcEx.ErrorCode : "Unknown"
                        }
                    };
                }

                // Continue to next model
            }
        }

        // Fallback return (shouldn't reach here)
        return new AIResponse
        {
            Success = false,
            ErrorMessage = "AWS Bedrock service error: No models could be accessed",
            ProcessingTime = DateTime.UtcNow - startTime
        };
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

    private InvokeModelRequest CreateBedrockRequest(AIRequest request, string? modelId = null)
    {
        var targetModelId = modelId ?? _options.ModelId;
        var prompt = !string.IsNullOrEmpty(request.Prompt)
            ? request.Prompt
            : BuildCaseAnalysisPrompt(request.DocumentText, request.CaseContext);

        object bedrockPayload;

        // Use different request formats based on model type
        if (targetModelId.Contains("claude-v2") || targetModelId.Contains("claude-instant"))
        {
            // Legacy Claude v2/Instant format
            bedrockPayload = new
            {
                prompt = $"\n\nHuman: {prompt}\n\nAssistant:",
                max_tokens_to_sample = request.MaxTokens,
                temperature = request.Temperature,
                stop_sequences = new[] { "\n\nHuman:" }
            };
        }
        else
        {
            // Claude 3+ message format
            bedrockPayload = new
            {
                anthropic_version = "bedrock-2023-05-31",
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };
        }

        return new InvokeModelRequest
        {
            ModelId = targetModelId,
            ContentType = "application/json",
            Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(bedrockPayload)))
        };
    }

    private AIResponse ProcessBedrockResponse(InvokeModelResponse response)
    {
        using var streamReader = new System.IO.StreamReader(response.Body);
        var responseText = streamReader.ReadToEnd();

        _logger.LogInformation("Processing Bedrock response - Raw response length: {Length}, First 200 chars: {Preview}",
            responseText.Length, responseText.Length > 200 ? responseText.Substring(0, 200) : responseText);

        try
        {
            // Try Claude 3 format first
            var claude3Response = JsonSerializer.Deserialize<Claude3Response>(responseText);
            if (claude3Response?.Content != null && claude3Response.Content.Length > 0)
            {
                _logger.LogInformation("Successfully parsed Claude 3 response with {ContentCount} content items",
                    claude3Response.Content.Length);
                return new AIResponse
                {
                    Success = true,
                    GeneratedText = claude3Response.Content[0].Text?.Trim(),
                    TokensUsed = claude3Response.Usage?.OutputTokens ?? 0,
                    ConfidenceScore = 0.85
                };
            }

            _logger.LogWarning("Claude 3 parsing failed or no content, trying Claude 2 format");

            // Fallback to Claude 2 format
            var bedrockResponse = JsonSerializer.Deserialize<BedrockCompletionResponse>(responseText);

            if (bedrockResponse != null)
            {
                _logger.LogInformation("Successfully parsed Claude 2 response");
                return new AIResponse
                {
                    Success = true,
                    GeneratedText = bedrockResponse.Completion?.Trim(),
                    TokensUsed = bedrockResponse.Completion?.Length / 4 ?? 0, // Rough estimate
                    ConfidenceScore = 0.85
                };
            }

            _logger.LogError("Failed to parse both Claude 3 and Claude 2 formats");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "Failed to parse response from AWS Bedrock",
                ProcessingTime = TimeSpan.Zero
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing failed, using raw response fallback");
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

    private class Claude3Response
    {
        public Claude3Content[]? Content { get; set; }
        public Claude3Usage? Usage { get; set; }
    }

    private class Claude3Content
    {
        public string? Text { get; set; }
        public string? Type { get; set; }
    }

    private class Claude3Usage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}