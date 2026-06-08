namespace Pagination_Project.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml);
    }
}