using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class EmpleadoService : IEmpleadoService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public EmpleadoService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Empleados>> ObtenerTodosAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Empleados
                .AsNoTracking()
                .OrderBy(e => e.Nombre)
                .ToListAsync();
        }

        public async Task<Empleados?> ObtenerPorIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Empleados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Empleados?> ObtenerPorIdEmpleadoAsync(int idEmpleado)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Empleados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpleado == idEmpleado);
        }

        public async Task CrearAsync(EmpleadoCreateDto dto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existeEmpleadoId = await context.Empleados
                .AnyAsync(e => e.IdEmpleado == dto.IdEmpleado);

            if (existeEmpleadoId)
                throw new Exception("The Employee ID is already registered.");

            var empleado = new Empleados
            {
                Id = Guid.NewGuid(),
                Nombre = dto.Nombre,
                IdEmpleado = dto.IdEmpleado,
                Email = dto.Email,
                Activo = dto.Activo
            };

            context.Empleados.Add(empleado);
            await context.SaveChangesAsync();
        }

        public async Task<Empleados?> ActualizarAsync(EmpleadoUpdateDto dto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var empleado = await context.Empleados
                .FirstOrDefaultAsync(e => e.Id == dto.Id);

            if (empleado == null)
                return null;

            var duplicado = await context.Empleados
                .AnyAsync(e => e.IdEmpleado == dto.IdEmpleado && e.Id != dto.Id);

            if (duplicado)
                throw new Exception("The Employee ID is already registered.");

            empleado.Nombre = dto.Nombre;
            empleado.IdEmpleado = dto.IdEmpleado;
            empleado.Email = dto.Email;
            empleado.Activo = dto.Activo;

            await context.SaveChangesAsync();
            return empleado;
        }

        public async Task<bool> EliminarAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var empleado = await context.Empleados
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empleado == null)
                return false;

            context.Empleados.Remove(empleado);
            await context.SaveChangesAsync();

            return true;
        }
    }
}