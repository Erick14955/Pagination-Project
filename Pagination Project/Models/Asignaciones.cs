using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    [Table("Assignments")]
    public class Asignaciones
    {
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }

        [Column("Employee_Id")]
        public Guid IdEmpleado { get; set; }

        [Column("Book_Id")]
        public Guid IdLibro { get; set; }

        public Empleados? Empleado { get; set; }

        public Libros? Libro { get; set; }
        public bool Finalizado { get; set; }

        public ICollection<Evaluaciones> Evaluaciones { get; set; } = new List<Evaluaciones>();
    }
}