using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    [Table("Permission_Level")]
    public class Permisos
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        public bool CreateUser { get; set; }
        public bool EditUser { get; set; }
        public bool DeleteUser { get; set; }
        public bool CreateBook { get; set; }
        public bool EditBook { get; set; }
        public bool DeleteBook { get; set; }
        public bool AsignBook { get; set; }
        public bool BooksView { get; set; }
        public bool QualifyBook { get; set; }
        public bool CreateEmployees { get; set; }
        public bool EditEmployees { get; set; }
        public bool DeleteEmployees { get; set; }
        public bool EditPermissionLevels { get; set; }
        public bool ViewAssignations { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}