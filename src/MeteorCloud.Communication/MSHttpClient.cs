using System.Net.Http.Json;
using MeteorCloud.Shared.ApiResults;
using Microsoft.Extensions.Logging;

namespace MeteorCloud.Communication;

public class MSHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MSHttpClient> _logger;
    
    public MSHttpClient(HttpClient httpClient, ILogger<MSHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<T>>();
            return apiResult ?? new ApiResult<T>(default, false, "Invalid API response format.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error calling GET {Url}: {Error}", url, ex.Message);
            return new ApiResult<T>(default, false, ex.Message);
        }
    }

    public async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, request);

            // Try reading the API response even if it's a failure (e.g., 400 Bad Request)
            var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<TResponse>>();
            if (apiResult is not null)
            {
                return apiResult;
            }

            // If API didn't return expected format, create a meaningful error response
            return new ApiResult<TResponse>(default, false, $"Unexpected API response. Status Code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error calling POST {Url}: {Error}", url, ex.Message);
            return new ApiResult<TResponse>(default, false, ex.Message);
        }
    }
}