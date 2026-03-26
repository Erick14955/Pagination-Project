using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public AuthService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Usuario?> ValidarLoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var usernameLimpio = username.Trim();

            await using var db = await _dbFactory.CreateDbContextAsync();

            var usuario = await db.Users
                .Include(u => u.Permisos)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == usernameLimpio);

            if (usuario is null || string.IsNullOrWhiteSpace(usuario.password))
                return null;

            try
            {
                return Argon2.Verify(usuario.password, password) ? usuario : null;
            }
            catch
            {
                return null;
            }
        }
    }
}