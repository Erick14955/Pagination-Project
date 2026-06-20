using DocumentFormat.OpenXml.InkML;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public UsuarioService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<bool> DesbloquearUsuarioAsync(Guid usuarioId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var usuario = await db.Users
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario is null)
                return false;

            usuario.LoginFailedAttempts = 0;
            usuario.LoginBloqueado = false;
            usuario.LoginBloqueadoAt = null;

            await db.SaveChangesAsync();

            return true;
        }

        public async Task<List<UsuarioListDto>> ObtenerTodosAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .Include(u => u.Permisos)
                .Include(u => u.Empleado)
                .OrderBy(u => u.Username)
                .Select(u => new UsuarioListDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.email,
                    Name = u.Name,
                    Lvl_Id = u.lvl_Id,
                    NivelNombre = u.Permisos != null ? u.Permisos.Name : string.Empty,
                    Activo = u.Activo,
                    EmployeeId = u.EmployeeId,
                    EmployeeCode = u.Empleado != null ? u.Empleado.IdEmpleado : null,
                    EmployeeName = u.Empleado != null ? u.Empleado.Nombre : string.Empty,
                    RequirePasswordChange = u.RequirePasswordChange,

                    LoginFailedAttempts = u.LoginFailedAttempts,
                    LoginBloqueado = u.LoginBloqueado,
                    LoginBloqueadoAt = u.LoginBloqueadoAt
                })
                .ToListAsync();
        }

        public async Task<PerfilUsuarioDto?> ObtenerPerfilActualAsync(Guid userId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .Include(u => u.Permisos)
                .Where(u => u.Id == userId)
                .Select(u => new PerfilUsuarioDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Name = u.Name,
                    NivelNombre = u.Permisos != null ? u.Permisos.Name : "No role",
                    ThemePreference = u.ThemePreference ?? "light"
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PerfilUsuarioDto?> ObtenerPerfilActualPorUsernameAsync(string username)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            username = username.Trim();

            return await context.Users
                .AsNoTracking()
                .Include(u => u.Permisos)
                .Where(u => u.Username.ToLower() == username.ToLower())
                .Select(u => new PerfilUsuarioDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Name = u.Name,
                    NivelNombre = u.Permisos != null ? u.Permisos.Name : "No role",
                    ThemePreference = u.ThemePreference ?? "light"
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CambiarPasswordPerfilAsync(CambiarPasswordPerfilDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (dto.UserId == Guid.Empty)
                throw new Exception("Invalid user.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new Exception("Password is required.");

            if (dto.NewPassword.Length < 6)
                throw new Exception("Password must have at least 6 characters.");

            var usuario = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);

            if (usuario == null)
                return false;

            usuario.password = Argon2.Hash(dto.NewPassword);
            usuario.RequirePasswordChange = false;

            await db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CambiarPasswordAsync(Guid usuarioId, string nuevaPassword)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var usuario = await db.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                return false;

            usuario.password = Argon2.Hash(nuevaPassword);
            usuario.RequirePasswordChange = false;

            await db.SaveChangesAsync();

            return true;
        }

        public async Task<List<PermisoComboDto>> ObtenerPermisosAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.PermissionLevels
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new PermisoComboDto
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToListAsync();
        }

        public async Task<UsuarioListDto> CrearAsync(UsuarioCreateDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            dto.Username = dto.Username.Trim();
            dto.Email = dto.Email.Trim().ToLower();
            dto.Name = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dto.Username))
                throw new Exception("Username is required.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new Exception("Email is required.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new Exception("Password is required.");

            if (dto.Lvl_Id <= 0)
                throw new Exception("You must select a user type.");

            var existeUsername = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower());

            if (existeUsername)
                throw new Exception("A user with that username already exists.");

            var existeEmail = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.email.ToLower() == dto.Email.ToLower());

            if (existeEmail)
                throw new Exception("A user with that email already exists.");

            var permisoExiste = await db.PermissionLevels
                .AsNoTracking()
                .AnyAsync(p => p.Id == dto.Lvl_Id);

            if (!permisoExiste)
                throw new Exception("The selected permission level does not exist.");

            if (dto.EmployeeId.HasValue)
            {
                var empleadoExiste = await db.Empleados
                    .AnyAsync(e => e.Id == dto.EmployeeId.Value);

                if (!empleadoExiste)
                    throw new Exception("The selected employee does not exist.");

                var empleadoYaTieneUsuario = await db.Users
                    .AnyAsync(u => u.EmployeeId == dto.EmployeeId.Value);

                if (empleadoYaTieneUsuario)
                    throw new Exception("This employee already has an assigned user.");
            }

            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = dto.Username.Trim().ToLowerInvariant(),
                email = dto.Email,
                password = Argon2.Hash(dto.Password),
                Name = dto.Name,
                lvl_Id = dto.Lvl_Id,
                Activo = dto.Activo,
                EmployeeId = dto.EmployeeId,
                RequirePasswordChange = dto.RequirePasswordChange
            };

            db.Users.Add(usuario);
            await db.SaveChangesAsync();

            var permisoNombre = await db.PermissionLevels
                .AsNoTracking()
                .Where(p => p.Id == usuario.lvl_Id)
                .Select(p => p.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            return new UsuarioListDto
            {
                Id = usuario.Id,
                Username = usuario.Username.Trim().ToLowerInvariant(),
                Email = usuario.email,
                Name = usuario.Name,
                Lvl_Id = usuario.lvl_Id,
                NivelNombre = permisoNombre,
                Activo = usuario.Activo
            };
        }

        public async Task<UsuarioListDto?> ActualizarAsync(UsuarioUpdateDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            dto.Username = dto.Username.Trim().ToLowerInvariant();
            dto.Email = dto.Email.Trim().ToLower();
            dto.Name = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dto.Username))
                throw new Exception("Username is required.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new Exception("Email is required.");

            if (dto.Lvl_Id <= 0)
                throw new Exception("You must select a user type.");

            var usuario = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.Id);

            if (usuario == null)
                return null;

            var existeUsername = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id != dto.Id && u.Username.ToLower() == dto.Username.ToLower());

            if (existeUsername)
                throw new Exception("Another user with that username already exists.");

            var existeEmail = await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id != dto.Id && u.email.ToLower() == dto.Email.ToLower());

            if (existeEmail)
                throw new Exception("Another user with that email already exists.");

            var permisoExiste = await db.PermissionLevels
                .AsNoTracking()
                .AnyAsync(p => p.Id == dto.Lvl_Id);

            if (!permisoExiste)
                throw new Exception("The selected permission level does not exist.");

            if (dto.EmployeeId.HasValue)
            {
                var empleadoExiste = await db.Empleados
                    .AnyAsync(e => e.Id == dto.EmployeeId.Value);

                if (!empleadoExiste)
                    throw new Exception("The selected employee does not exist.");

                var empleadoYaTieneOtroUsuario = await db.Users
                    .AnyAsync(u => u.EmployeeId == dto.EmployeeId.Value && u.Id != dto.Id);

                if (empleadoYaTieneOtroUsuario)
                    throw new Exception("This employee already has an assigned user.");
            }

            usuario.Username = dto.Username.Trim().ToLowerInvariant();
            usuario.email = dto.Email;
            usuario.Name = dto.Name;
            usuario.lvl_Id = dto.Lvl_Id;
            usuario.Activo = dto.Activo;
            usuario.EmployeeId = dto.EmployeeId;
            usuario.RequirePasswordChange = dto.RequirePasswordChange;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                usuario.password = Argon2.Hash(dto.Password);
            }

            await db.SaveChangesAsync();

            var permisoNombre = await db.PermissionLevels
                .AsNoTracking()
                .Where(p => p.Id == usuario.lvl_Id)
                .Select(p => p.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            return new UsuarioListDto
            {
                Id = usuario.Id,
                Username = usuario.Username.Trim().ToLowerInvariant(),
                Email = usuario.email,
                Name = usuario.Name,
                Lvl_Id = usuario.lvl_Id,
                NivelNombre = permisoNombre,
                Activo = usuario.Activo,
                EmployeeId = usuario.EmployeeId
            };
        }

        public async Task<bool> EliminarAsync(Guid id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var usuario = await db.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return false;

            db.Users.Remove(usuario);
            await db.SaveChangesAsync();

            return true;
        }

        public async Task<List<EmpleadoUsuarioComboDto>> ObtenerEmpleadosDisponiblesAsync(Guid? usuarioActualId = null)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            Guid? empleadoActualId = null;

            if (usuarioActualId.HasValue)
            {
                empleadoActualId = await context.Users
                    .Where(u => u.Id == usuarioActualId.Value)
                    .Select(u => u.EmployeeId)
                    .FirstOrDefaultAsync();
            }

            var empleadosUsados = context.Users
                .Where(u => u.EmployeeId != null)
                .Where(u => !usuarioActualId.HasValue || u.Id != usuarioActualId.Value)
                .Select(u => u.EmployeeId!.Value);

            return await context.Empleados
                .AsNoTracking()
                .Where(e => e.Activo)
                .Where(e => !empleadosUsados.Contains(e.Id) || e.Id == empleadoActualId)
                .OrderBy(e => e.Nombre)
                .Select(e => new EmpleadoUsuarioComboDto
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    IdEmpleado = e.IdEmpleado,
                    Email = e.Email
                })
                .ToListAsync();
        }

        public async Task<bool> ActualizarThemePreferenceAsync(Guid userId, string themePreference)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (userId == Guid.Empty)
                throw new Exception("Invalid user.");

            if (string.IsNullOrWhiteSpace(themePreference))
                themePreference = "light";

            themePreference = themePreference.Trim().ToLower();

            var temasPermitidos = new[]
            {
                "light",
                "dark",
                "liquid glass",
                "dark glass"
            };

            if (!temasPermitidos.Contains(themePreference))
                throw new Exception("Invalid theme preference.");

            var usuario = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return false;

            usuario.ThemePreference = themePreference;

            await db.SaveChangesAsync();

            return true;
        }
    }
}