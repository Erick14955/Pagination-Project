using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public interface IAuthService
    {
        Task<Usuario?> ValidarLoginAsync(string username, string password);
    }
}