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
            var resultado = await ValidarLoginDetalladoAsync(username, password);

            return resultado.Exitoso ? resultado.Usuario : null;
        }

        public async Task<LoginResult> ValidarLoginDetalladoAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new LoginResult
                {
                    Estado = LoginEstado.UsuarioNoExiste
                };
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult
                {
                    Estado = LoginEstado.ContrasenaIncorrecta
                };
            }

            var usernameLimpio = username.Trim();

            await using var db = await _dbFactory.CreateDbContextAsync();

            var usuario = await db.Users
                .Include(u => u.Permisos)
                .Include(u => u.Empleado)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == usernameLimpio);

            if (usuario is null)
            {
                return new LoginResult
                {
                    Estado = LoginEstado.UsuarioNoExiste
                };
            }

            if (usuario.Activo == false)
            {
                return new LoginResult
                {
                    Estado = LoginEstado.UsuarioInactivo,
                    Usuario = usuario
                };
            }

            if (string.IsNullOrWhiteSpace(usuario.password))
            {
                return new LoginResult
                {
                    Estado = LoginEstado.ContrasenaIncorrecta,
                    Usuario = usuario
                };
            }

            try
            {
                var passwordCorrecta = Argon2.Verify(usuario.password, password);

                if (!passwordCorrecta)
                {
                    return new LoginResult
                    {
                        Estado = LoginEstado.ContrasenaIncorrecta,
                        Usuario = usuario
                    };
                }

                return new LoginResult
                {
                    Estado = LoginEstado.Correcto,
                    Usuario = usuario
                };
            }
            catch
            {
                return new LoginResult
                {
                    Estado = LoginEstado.ContrasenaIncorrecta,
                    Usuario = usuario
                };
            }
        }
    }
}