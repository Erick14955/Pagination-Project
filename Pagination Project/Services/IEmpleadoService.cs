using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public interface IEmpleadoService
    {
        Task<List<Empleados>> ObtenerTodosAsync(UserDataScope scope);
        Task<Empleados?> ObtenerPorIdAsync(Guid id, UserDataScope scope);
        Task<Empleados?> ObtenerPorIdEmpleadoAsync(int idEmpleado, UserDataScope scope);
        Task CrearAsync(EmpleadoCreateDto dto, UserDataScope scope);
        Task<Empleados?> ActualizarAsync(EmpleadoUpdateDto dto, UserDataScope scope);
        Task<bool> EliminarAsync(Guid id, UserDataScope scope);
    }
}
