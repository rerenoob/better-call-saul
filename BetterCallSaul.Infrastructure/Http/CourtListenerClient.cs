using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BetterCallSaul.Infrastructure.Http;

public class CourtListenerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CourtListenerClient> _logger;
    private const string BaseUrl = "https://www.courtlistener.com/api/rest/v3";

    public CourtListenerClient(HttpClient httpClient, ILogger<CourtListenerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "BetterCallSaul/1.0");
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while calling CourtListener API");
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while calling CourtListener API");
            throw;
        }
    }
}