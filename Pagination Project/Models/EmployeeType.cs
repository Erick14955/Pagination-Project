namespace Pagination_Project.Models
{
    public sealed class EmployeeType
    {
        public short Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; } = true;

        public ICollection<Empleados> Employees { get; set; } = new List<Empleados>();
        public ICollection<Libros> Books { get; set; } = new List<Libros>();
    }
}
