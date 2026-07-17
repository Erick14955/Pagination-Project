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

        public async Task<List<Empleados>> ObtenerTodosAsync(UserDataScope scope)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Empleados
                .AsNoTracking()
                .Include(e => e.EmployeeType)
                .ApplyScope(scope)
                .OrderBy(e => e.Nombre)
                .ToListAsync();
        }

        public async Task<Empleados?> ObtenerPorIdAsync(Guid id, UserDataScope scope)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Empleados
                .AsNoTracking()
                .Include(e => e.EmployeeType)
                .ApplyScope(scope)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Empleados?> ObtenerPorIdEmpleadoAsync(int idEmpleado, UserDataScope scope)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Empleados
                .AsNoTracking()
                .Include(e => e.EmployeeType)
                .ApplyScope(scope)
                .FirstOrDefaultAsync(e => e.IdEmpleado == idEmpleado);
        }

        public async Task CrearAsync(EmpleadoCreateDto dto, UserDataScope scope)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            ValidateEmployeeType(dto.EmployeeTypeId, scope);

            var typeExists = await context.EmployeeTypes
                .AnyAsync(x => x.Id == dto.EmployeeTypeId && x.Active);

            if (!typeExists)
                throw new Exception("The selected employee type does not exist or is inactive.");

            var existeEmpleadoId = await context.Empleados
                .AnyAsync(e => e.IdEmpleado == dto.IdEmpleado);

            if (existeEmpleadoId)
                throw new Exception("The Employee ID is already registered.");

            var empleado = new Empleados
            {
                Id = Guid.NewGuid(),
                Nombre = dto.Nombre.Trim(),
                IdEmpleado = dto.IdEmpleado,
                Email = dto.Email.Trim(),
                Activo = dto.Activo,
                EmployeeTypeId = dto.EmployeeTypeId
            };

            context.Empleados.Add(empleado);
            await context.SaveChangesAsync();
        }

        public async Task<Empleados?> ActualizarAsync(EmpleadoUpdateDto dto, UserDataScope scope)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            ValidateEmployeeType(dto.EmployeeTypeId, scope);

            var empleado = await context.Empleados
                .ApplyScope(scope)
                .FirstOrDefaultAsync(e => e.Id == dto.Id);

            if (empleado == null)
                return null;

            if (empleado.EmployeeTypeId != dto.EmployeeTypeId)
            {
                var hasAssignments = await context.Asignaciones
                    .AnyAsync(a => a.IdEmpleado == empleado.Id);

                if (hasAssignments)
                {
                    throw new Exception(
                        "The employee type cannot be changed because this employee already has assignments.");
                }
            }

            var typeExists = await context.EmployeeTypes
                .AnyAsync(x => x.Id == dto.EmployeeTypeId && x.Active);

            if (!typeExists)
                throw new Exception("The selected employee type does not exist or is inactive.");

            var duplicado = await context.Empleados
                .AnyAsync(e => e.IdEmpleado == dto.IdEmpleado && e.Id != dto.Id);

            if (duplicado)
                throw new Exception("The Employee ID is already registered.");

            empleado.Nombre = dto.Nombre.Trim();
            empleado.IdEmpleado = dto.IdEmpleado;
            empleado.Email = dto.Email.Trim();
            empleado.Activo = dto.Activo;
            empleado.EmployeeTypeId = dto.EmployeeTypeId;

            await context.SaveChangesAsync();
            return empleado;
        }

        public async Task<bool> EliminarAsync(Guid id, UserDataScope scope)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var empleado = await context.Empleados
                .ApplyScope(scope)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empleado == null)
                return false;

            context.Empleados.Remove(empleado);
            await context.SaveChangesAsync();

            return true;
        }

        private static void ValidateEmployeeType(short employeeTypeId, UserDataScope scope)
        {
            if (employeeTypeId <= 0)
                throw new Exception("You must select an employee type.");

            if (!scope.CanAccess(employeeTypeId))
                throw new UnauthorizedAccessException("You cannot manage employees from another employee type.");
        }
    }
}
