namespace BetterCallSaul.Gateway.Services
{
    public interface IGatewayService
    {
        Task<HttpResponseMessage> ForwardRequestAsync(HttpRequest request, string serviceName, string targetPath);
        Task<bool> ValidateJwtTokenAsync(string token);
        Task<string> ExtractUserIdFromTokenAsync(string token);
    }
}