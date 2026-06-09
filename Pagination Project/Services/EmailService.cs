using System.Net;
using System.Net.Mail;

namespace Pagination_Project.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var host = _configuration["Email:SmtpHost"];
            var port = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var from = _configuration["Email:From"];
            var fromName = _configuration["Email:FromName"] ?? "Evaluations";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException("Email SMTP configuration is incomplete.");
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                Timeout = 20000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };

            message.To.Add(destinatario);

            Console.WriteLine($"Enviando correo a: {destinatario}");
            Console.WriteLine($"SMTP Host: {_configuration["Email:SmtpHost"]}");
            Console.WriteLine($"SMTP User: {_configuration["Email:Username"]}");
            Console.WriteLine($"SMTP From: {_configuration["Email:From"]}");

            await client.SendMailAsync(message);
        }
    }
}