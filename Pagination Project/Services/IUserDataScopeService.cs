using System.Security.Claims;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public interface IUserDataScopeService
    {
        Task<UserDataScope> GetScopeAsync(ClaimsPrincipal principal);
    }
}
