using System.Text.Json;
using Microsoft.Extensions.Options;
using better_call_saul.Models;

namespace better_call_saul.Services;

public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.3;
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAIOptions _options;
    private readonly ILoggerService _logger;
    private readonly HttpClient _httpClient;

    public AzureOpenAIService(IOptions<AzureOpenAIOptions> options, ILoggerService logger, HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogWarning("Azure OpenAI configuration incomplete. Service will not be available.");
        }
        else
        {
            _logger.LogInformation("Azure OpenAI service initialized");
        }
    }

    public async Task<AIAnalysisResult> AnalyzeDocumentAsync(string documentText, string analysisType)
    {
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            return new AIAnalysisResult
            {
                Success = false,
                ErrorMessage = "Azure OpenAI service not configured"
            };
        }

        try
        {
            _logger.LogInformation($"Starting document analysis for type: {analysisType}");

            var systemPrompt = GetSystemPromptForAnalysisType(analysisType);
            var userPrompt = $"Document content:\n{documentText}";

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = _options.MaxTokens,
                temperature = _options.Temperature,
                deployment_id = _options.DeploymentName
            };

            var response = await MakeOpenAIRequest(requestBody);
            var result = response.Choices[0].Message.Content;

            return ParseAnalysisResult(result, analysisType);
        }
        catch (Exception ex) when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
        {
            _logger.LogError($"Azure OpenAI authentication failed: {ex.Message}", ex);
            return new AIAnalysisResult
            {
                Success = false,
                ErrorMessage = "Authentication failed. Please check API configuration."
            };
        }
        catch (Exception ex) when (ex.Message.Contains("429") || ex.Message.Contains("rate limit"))
        {
            _logger.LogWarning($"Azure OpenAI rate limit exceeded: {ex.Message}");
            return new AIAnalysisResult
            {
                Success = false,
                ErrorMessage = "Rate limit exceeded. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during document analysis: {ex.Message}", ex);
            return new AIAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Analysis failed: {ex.Message}"
            };
        }
    }

    public async Task<string> GenerateSummaryAsync(string documentText)
    {
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            return "Azure OpenAI service not configured";
        }

        try
        {
            _logger.LogInformation("Generating document summary");

            var systemPrompt = "You are a legal document assistant. Create a concise summary of the provided legal document, focusing on key points, parties involved, and main obligations or rights.";
            var userPrompt = $"Please summarize this document:\n{documentText}";

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 500,
                temperature = 0.2,
                deployment_id = _options.DeploymentName
            };

            var response = await MakeOpenAIRequest(requestBody);
            return response.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating document summary: {ex.Message}", ex);
            return $"Failed to generate summary: {ex.Message}";
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            return false;
        }

        try
        {
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                },
                max_tokens = 5,
                deployment_id = _options.DeploymentName
            };

            await MakeOpenAIRequest(requestBody);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<OpenAIResponse> MakeOpenAIRequest(object requestBody)
    {
        var url = $"{_options.Endpoint}/openai/deployments/{_options.DeploymentName}/chat/completions?api-version=2024-02-15-preview";
        
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Add("Content-Type", "application/json");
        
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OpenAIResponse>(content) ?? throw new Exception("Failed to deserialize OpenAI response");
    }

    private string GetSystemPromptForAnalysisType(string analysisType)
    {
        return analysisType.ToLower() switch
        {
            "contract" => "You are a legal contract analysis expert. Analyze the provided contract and provide: 1. A concise summary, 2. Key obligations and rights, 3. Potential risks or concerns, 4. Recommendations. Format your response with clear sections.",
            "motion" => "You are a legal motion analysis expert. Analyze the provided motion and provide: 1. Summary of the motion, 2. Legal basis and arguments, 3. Strengths and weaknesses, 4. Recommended response strategy.",
            "pleading" => "You are a legal pleading analysis expert. Analyze the provided pleading and provide: 1. Summary of claims/defenses, 2. Legal sufficiency assessment, 3. Potential counterarguments, 4. Recommendations for response.",
            _ => "You are a legal document analysis expert. Analyze the provided document and provide: 1. A concise summary, 2. Key points and findings, 3. Recommendations or next steps."
        };
    }

    private AIAnalysisResult ParseAnalysisResult(string result, string analysisType)
    {
        try
        {
            var analysisResult = new AIAnalysisResult
            {
                Success = true,
                ConfidenceScore = 0.8
            };

            var lines = result.Split('\n');
            var summaryBuilder = new List<string>();
            var keyPoints = new List<string>();
            var recommendationBuilder = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("1.", StringComparison.OrdinalIgnoreCase))
                {
                    summaryBuilder.Add(line);
                }
                else if (line.StartsWith("Key points:", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("2.", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("-", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("*", StringComparison.OrdinalIgnoreCase))
                {
                    keyPoints.Add(line.TrimStart('-', '*', ' '));
                }
                else if (line.StartsWith("Recommendation:", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("3.", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("4.", StringComparison.OrdinalIgnoreCase))
                {
                    recommendationBuilder.Add(line);
                }
            }

            analysisResult.Summary = string.Join("\n", summaryBuilder);
            analysisResult.KeyPoints = keyPoints;
            analysisResult.Recommendation = string.Join("\n", recommendationBuilder);

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing AI analysis result: {ex.Message}", ex);
            return new AIAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse analysis result: {ex.Message}"
            };
        }
    }

    private class OpenAIResponse
    {
        public List<Choice> Choices { get; set; } = new();
    }

    private class Choice
    {
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}