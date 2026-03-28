using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public interface IUsuarioService
    {
        Task<List<UsuarioListDto>> ObtenerTodosAsync();
        Task<List<PermisoComboDto>> ObtenerPermisosAsync();
        Task<UsuarioListDto> CrearAsync(UsuarioCreateDto dto);
        Task<UsuarioListDto?> ActualizarAsync(UsuarioUpdateDto dto);
        Task<bool> EliminarAsync(Guid id);
    }
}