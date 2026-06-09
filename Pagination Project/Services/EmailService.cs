using System.Text;
using System.Text.Json;

namespace Pagination_Project.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"] ?? "Evaluations";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Brevo API configuration is incomplete.");

            var payload = new
            {
                sender = new
                {
                    name = fromName,
                    email = from
                },
                to = new[]
                {
                    new { email = destinatario }
                },
                subject = asunto,
                htmlContent = cuerpoHtml,

                headers = new Dictionary<string, string>
                {
                    { "X-Mailin-track", "0" }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email");

            request.Headers.Add("api-key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Brevo API error: {response.StatusCode} - {error}");
            }
        }
    }
}