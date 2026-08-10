using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public sealed class UserDataScopeService : IUserDataScopeService
    {
        private static readonly TimeSpan ScopeCacheDuration =
            TimeSpan.FromMinutes(1);

        private const string ScopeCachePrefix =
            "pagination-user-data-scope";

        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IMemoryCache _cache;

        public UserDataScopeService(
            IDbContextFactory<AppDbContext> dbFactory,
            IMemoryCache cache)
        {
            _dbFactory = dbFactory;
            _cache = cache;
        }

        public async Task<UserDataScope> GetScopeAsync(
            ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(
                    "The user is not authenticated.");
            }

            var userIdValue =
                principal.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(
                    userIdValue,
                    out var userId) ||
                userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user identifier is invalid.");
            }

            var cacheKey =
                ObtenerClaveCache(userId);

            if (_cache.TryGetValue<UserDataScope>(
                    cacheKey,
                    out var cachedScope) &&
                cachedScope is not null)
            {
                return ClonarScope(cachedScope);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var scope = await db.Users
                .Where(u =>
                    u.Id == userId &&
                    u.Activo &&
                    !u.LoginBloqueado)
                .Select(u => new UserDataScope
                {
                    UserId = u.Id,

                    EmployeeId = u.EmployeeId,

                    EmployeeTypeId =
                        u.Empleado != null
                            ? u.Empleado.EmployeeTypeId
                            : null,

                    EmployeeTypeCode =
                        u.Empleado != null &&
                        u.Empleado.EmployeeType != null
                            ? u.Empleado.EmployeeType.Code
                            : string.Empty,

                    ViewAllEmployeeTypes =
                        u.Permisos != null &&
                        u.Permisos.ViewAllEmployeeTypes
                })
                .FirstOrDefaultAsync();

            if (scope is null)
            {
                _cache.Remove(cacheKey);

                throw new UnauthorizedAccessException(
                    "The current user is not active or no longer exists.");
            }

            if (!scope.ViewAllEmployeeTypes &&
                !scope.EmployeeTypeId.HasValue)
            {
                _cache.Remove(cacheKey);

                throw new UnauthorizedAccessException(
                    "This user must be linked to an employee with an employee type before accessing departmental data.");
            }

            var scopeParaCache =
                ClonarScope(scope);

            _cache.Set(
                cacheKey,
                scopeParaCache,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        ScopeCacheDuration
                });

            return ClonarScope(scope);
        }

        private static string ObtenerClaveCache(
            Guid userId)
        {
            return
                $"{ScopeCachePrefix}:{userId:N}";
        }

        private static UserDataScope ClonarScope(
            UserDataScope source)
        {
            return new UserDataScope
            {
                UserId = source.UserId,

                EmployeeId = source.EmployeeId,

                EmployeeTypeId =
                    source.EmployeeTypeId,

                EmployeeTypeCode =
                    source.EmployeeTypeCode
                    ?? string.Empty,

                ViewAllEmployeeTypes =
                    source.ViewAllEmployeeTypes
            };
        }
    }
}