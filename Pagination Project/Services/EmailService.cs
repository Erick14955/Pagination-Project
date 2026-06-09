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
            var apiKey = _configuration["Brevo:ApiKey"]; ;
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"] ?? "Evaluations";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Resend API Key is missing.");

            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Email From is missing.");

            if (string.IsNullOrWhiteSpace(destinatario))
                throw new InvalidOperationException("Email recipient is missing.");

            var payload = new
            {
                from = $"{fromName} <{from}>",
                to = new[]
                {
                    destinatario
                },
                subject = asunto,
                html = cuerpoHtml
            };

            var json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email");

            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                Console.WriteLine("ERROR ENVIANDO CORREO CON RESEND:");
                Console.WriteLine(error);

                throw new Exception($"Resend API error: {response.StatusCode} - {error}");
            }
        }
    }
}