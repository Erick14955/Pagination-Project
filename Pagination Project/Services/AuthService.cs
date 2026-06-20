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

            if (usuario.LoginBloqueado)
            {
                return new LoginResult
                {
                    Estado = LoginEstado.CuentaBloqueada,
                    Usuario = usuario
                };
            }

            if (string.IsNullOrWhiteSpace(usuario.password))
            {
                await RegistrarIntentoFallidoAsync(db, usuario);

                return new LoginResult
                {
                    Estado = usuario.LoginBloqueado
                        ? LoginEstado.CuentaBloqueada
                        : LoginEstado.ContrasenaIncorrecta,
                    Usuario = usuario
                };
            }

            try
            {
                var passwordCorrecta = Argon2.Verify(usuario.password, password);

                if (!passwordCorrecta)
                {
                    await RegistrarIntentoFallidoAsync(db, usuario);

                    return new LoginResult
                    {
                        Estado = usuario.LoginBloqueado
                            ? LoginEstado.CuentaBloqueada
                            : LoginEstado.ContrasenaIncorrecta,
                        Usuario = usuario
                    };
                }

                if (usuario.LoginFailedAttempts > 0)
                {
                    usuario.LoginFailedAttempts = 0;
                    usuario.LoginBloqueado = false;
                    usuario.LoginBloqueadoAt = null;

                    await db.SaveChangesAsync();
                }

                return new LoginResult
                {
                    Estado = LoginEstado.Correcto,
                    Usuario = usuario
                };
            }
            catch
            {
                await RegistrarIntentoFallidoAsync(db, usuario);

                return new LoginResult
                {
                    Estado = usuario.LoginBloqueado
                        ? LoginEstado.CuentaBloqueada
                        : LoginEstado.ContrasenaIncorrecta,
                    Usuario = usuario
                };
            }
        }

        private static DateTime FechaLocalSinZona()
        {
            return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        }

        private static async Task RegistrarIntentoFallidoAsync(AppDbContext db, Usuario usuario)
        {
            usuario.LoginFailedAttempts++;

            if (usuario.LoginFailedAttempts >= 3)
            {
                usuario.LoginBloqueado = true;
                usuario.LoginBloqueadoAt = FechaLocalSinZona();
            }

            await db.SaveChangesAsync();
        }
    }
}