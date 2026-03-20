using System.Net.Http.Headers;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class SupabaseDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SupabaseDashboardService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private async Task<int> GetCountAsync(string tableName)
        {
            var baseUrl = _configuration["Supabase:BaseUrl"];
            var apiKey = _configuration["Supabase:ApiKey"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new Exception("No existe Supabase:BaseUrl en appsettings.json.");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("No existe Supabase:ApiKey en appsettings.json.");

            var url = $"{baseUrl}/rest/v1/{tableName}?select=ID";

            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            request.Headers.Add("apikey", apiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("Prefer", "count=exact");

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Tabla {tableName}: {response.StatusCode} - {body}");
            }

            if (response.Headers.TryGetValues("Content-Range", out var values))
            {
                var contentRange = values.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(contentRange))
                {
                    var parts = contentRange.Split('/');

                    if (parts.Length == 2 && int.TryParse(parts[1], out int total))
                        return total;
                }
            }

            return 0;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            return new DashboardStatsDto
            {
                TotalUsers = await GetCountAsync("Users"),
                TotalEmployees = await GetCountAsync("Employees"),
                TotalBooks = await GetCountAsync("Books"),
                TotalEvaluations = await GetCountAsync("Evaluations")
            };
        }
    }
}