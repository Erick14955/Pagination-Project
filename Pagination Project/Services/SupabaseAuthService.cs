using Pagination_Project.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class SupabaseAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SupabaseAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<SupabaseUserDto?> GetUserByUsernameAsync(string username)
        {
            var baseUrl = _configuration["Supabase:BaseUrl"];
            var apiKey = _configuration["Supabase:ApiKey"];

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Supabase configuration is not defined.");

            var url = $"{baseUrl}/rest/v1/Users?Username=eq.{Uri.EscapeDataString(username)}&select=Username,Password";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("apikey", apiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error querying supabase: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var users = JsonSerializer.Deserialize<List<SupabaseUserDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return users?.FirstOrDefault();
        }
    }
}