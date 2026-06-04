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
        Task<List<EmpleadoUsuarioComboDto>> ObtenerEmpleadosDisponiblesAsync(Guid? usuarioActualId = null);
        Task<bool> CambiarPasswordAsync(Guid usuarioId, string nuevaPassword);
        Task<PerfilUsuarioDto?> ObtenerPerfilActualAsync(Guid userId);

        Task<PerfilUsuarioDto?> ObtenerPerfilActualPorUsernameAsync(string username);

        Task<bool> CambiarPasswordPerfilAsync(CambiarPasswordPerfilDto dto);
        Task<bool> ActualizarThemePreferenceAsync(Guid userId, string themePreference);
    }
}