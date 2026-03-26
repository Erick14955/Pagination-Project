namespace Pagination_Project.Models
{
    public class Empleados
{
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int IdEmpleado { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public ICollection<Asignaciones> Asignaciones { get; set; } = new List<Asignaciones>();
    }
}
