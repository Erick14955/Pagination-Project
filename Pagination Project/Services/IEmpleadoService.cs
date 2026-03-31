using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public interface IEmpleadoService
    {
        Task<List<Empleados>> ObtenerTodosAsync();
        Task<Empleados?> ObtenerPorIdAsync(Guid id);
        Task<Empleados?> ObtenerPorIdEmpleadoAsync(int idEmpleado);
        Task CrearAsync(EmpleadoCreateDto dto);
        Task<Empleados?> ActualizarAsync(EmpleadoUpdateDto dto);
        Task<bool> EliminarAsync(Guid id);
    }
}