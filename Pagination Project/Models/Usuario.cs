using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    [Table("Users")]
    public class Usuario
    {
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        public string email { get; set; } = string.Empty;

        [Required]
        [Column("Password")]
        public string password { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        [Required]
        [Column("Active")]
        public bool Activo { get; set; } = true;

        [Column("Lvl_Id")]
        public int lvl_Id { get; set; }

        [ForeignKey(nameof(lvl_Id))]
        public Permisos? Permisos { get; set; }

        [Column("Employee_Id")]
        public Guid? EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Empleados? Empleado { get; set; }

        public bool RequirePasswordChange { get; set; } = false;
        public string ThemePreference { get; set; } = "light";
    }
}