using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
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

        public async Task<List<UsuarioListDto>> ObtenerTodosAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Users
                .AsNoTracking()
                .Include(u => u.Permisos)
                .OrderBy(u => u.Username)
                .Select(u => new UsuarioListDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.email,
                    Name = u.Name,
                    Lvl_Id = u.lvl_Id,
                    NivelNombre = u.Permisos != null ? u.Permisos.Name : string.Empty
                })
                .ToListAsync();
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

            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                email = dto.Email,
                password = Argon2.Hash(dto.Password),
                Name = dto.Name,
                lvl_Id = dto.Lvl_Id
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
                Username = usuario.Username,
                Email = usuario.email,
                Name = usuario.Name,
                Lvl_Id = usuario.lvl_Id,
                NivelNombre = permisoNombre
            };
        }

        public async Task<UsuarioListDto?> ActualizarAsync(UsuarioUpdateDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            dto.Username = dto.Username.Trim();
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

            usuario.Username = dto.Username;
            usuario.email = dto.Email;
            usuario.Name = dto.Name;
            usuario.lvl_Id = dto.Lvl_Id;

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
                Username = usuario.Username,
                Email = usuario.email,
                Name = usuario.Name,
                Lvl_Id = usuario.lvl_Id,
                NivelNombre = permisoNombre
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
    }
}