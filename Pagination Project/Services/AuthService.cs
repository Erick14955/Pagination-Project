using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class AuthService : IAuthService
    {
        private const int MaxFailedAttempts = 3;

        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        private static readonly TimeZoneInfo DominicanTimeZone =
            GetDominicanTimeZone();

        public AuthService(
            IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<bool> UsuarioSigueActivoAsync(
            Guid usuarioId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id == usuarioId &&
                    u.Activo == true &&
                    u.LoginBloqueado == false);
        }

        public async Task<Usuario?> ValidarLoginAsync(
            string username,
            string password)
        {
            var resultado =
                await ValidarLoginDetalladoAsync(
                    username,
                    password);

            return resultado.Exitoso
                ? resultado.Usuario
                : null;
        }

        public async Task<LoginResult> ValidarLoginDetalladoAsync(
            string username,
            string password)
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

            var usernameLimpio =
                username
                    .Trim()
                    .ToLowerInvariant();

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var usuario = await db.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == usernameLimpio);

            if (usuario is null)
            {
                usuario = await db.Users
                    .FirstOrDefaultAsync(u =>
                        u.Username != null &&
                        u.Username.ToLower() == usernameLimpio);
            }

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
                await RegistrarIntentoFallidoAsync(
                    db,
                    usuario);

                return new LoginResult
                {
                    Estado = usuario.LoginBloqueado
                        ? LoginEstado.CuentaBloqueada
                        : LoginEstado.ContrasenaIncorrecta,

                    Usuario = usuario
                };
            }

            bool passwordCorrecta;

            try
            {
                passwordCorrecta =
                    Argon2.Verify(
                        usuario.password,
                        password);
            }
            catch
            {
                passwordCorrecta = false;
            }

            if (!passwordCorrecta)
            {
                await RegistrarIntentoFallidoAsync(
                    db,
                    usuario);

                return new LoginResult
                {
                    Estado = usuario.LoginBloqueado
                        ? LoginEstado.CuentaBloqueada
                        : LoginEstado.ContrasenaIncorrecta,

                    Usuario = usuario
                };
            }

            if (usuario.LoginFailedAttempts > 0 ||
                usuario.LoginBloqueado ||
                usuario.LoginBloqueadoAt.HasValue)
            {
                usuario.LoginFailedAttempts = 0;
                usuario.LoginBloqueado = false;
                usuario.LoginBloqueadoAt = null;

                await db.SaveChangesAsync();
            }

            var usuarioCompleto = await db.Users
                .AsNoTracking()
                .Include(u => u.Permisos)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.EmployeeType)
                .FirstOrDefaultAsync(u =>
                    u.Id == usuario.Id);

            if (usuarioCompleto is null)
            {
                return new LoginResult
                {
                    Estado = LoginEstado.UsuarioNoExiste
                };
            }

            if (usuarioCompleto.Activo == false)
            {
                return new LoginResult
                {
                    Estado = LoginEstado.UsuarioInactivo,
                    Usuario = usuarioCompleto
                };
            }

            if (usuarioCompleto.LoginBloqueado)
            {
                return new LoginResult
                {
                    Estado = LoginEstado.CuentaBloqueada,
                    Usuario = usuarioCompleto
                };
            }

            return new LoginResult
            {
                Estado = LoginEstado.Correcto,
                Usuario = usuarioCompleto
            };
        }

        private static async Task RegistrarIntentoFallidoAsync(
            AppDbContext db,
            Usuario usuario)
        {
            usuario.LoginFailedAttempts++;

            if (usuario.LoginFailedAttempts >= MaxFailedAttempts)
            {
                usuario.LoginBloqueado = true;
                usuario.LoginBloqueadoAt =
                    FechaLocalDominicanaSinZona();
            }

            await db.SaveChangesAsync();
        }

        private static DateTime FechaLocalDominicanaSinZona()
        {
            var fechaDominicana =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    DominicanTimeZone);

            return DateTime.SpecifyKind(
                fechaDominicana,
                DateTimeKind.Unspecified);
        }

        private static TimeZoneInfo GetDominicanTimeZone()
        {

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Santo_Domingo");
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "SA Western Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }

            return TimeZoneInfo.Utc;
        }
    }
}