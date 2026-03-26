namespace Pagination_Project.Models
{
    public class Asignaciones
{
        public Guid Id { get; set; }
        public Guid IdEmpleado { get; set; }
        public Guid IdLibro { get; set; }
        public Empleados? Empleado { get; set; }
        public Libros? Libro { get; set; }
        public ICollection<Evaluaciones> Evaluaciones { get; set; } = new List<Evaluaciones>();
    }
}
