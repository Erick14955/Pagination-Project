using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class EmpleadoCreateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int IdEmpleado { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }

    public class EmpleadoUpdateDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int IdEmpleado { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}