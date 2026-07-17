using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public sealed class UserDataScopeService : IUserDataScopeService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public UserDataScopeService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<UserDataScope> GetScopeAsync(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException("The user is not authenticated.");

            var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
                throw new UnauthorizedAccessException("The authenticated user identifier is invalid.");

            await using var db = await _dbFactory.CreateDbContextAsync();

            var scope = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId && u.Activo && !u.LoginBloqueado)
                .Select(u => new UserDataScope
                {
                    UserId = u.Id,
                    EmployeeId = u.EmployeeId,
                    EmployeeTypeId = u.Empleado != null
                        ? u.Empleado.EmployeeTypeId
                        : null,
                    EmployeeTypeCode = u.Empleado != null && u.Empleado.EmployeeType != null
                        ? u.Empleado.EmployeeType.Code
                        : string.Empty,
                    ViewAllEmployeeTypes = u.Permisos != null &&
                                           u.Permisos.ViewAllEmployeeTypes
                })
                .FirstOrDefaultAsync();

            if (scope is null)
                throw new UnauthorizedAccessException("The current user is not active or no longer exists.");

            if (!scope.ViewAllEmployeeTypes && !scope.EmployeeTypeId.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "This user must be linked to an employee with an employee type before accessing departmental data.");
            }

            return scope;
        }
    }
}
