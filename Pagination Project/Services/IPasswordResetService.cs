namespace Pagination_Project.Services
{
    public interface IPasswordResetService
    {
        Task SolicitarRecuperacionAsync(string username, string baseUrl);
        Task<bool> RestablecerPasswordAsync(string token, string nuevaPassword);
    }
}