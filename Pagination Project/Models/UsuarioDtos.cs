using System.ComponentModel.DataAnnotations;

namespace Pagination_Project.Models
{
    public class UsuarioListDto
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Lvl_Id { get; set; }

        public string NivelNombre { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public Guid? EmployeeId { get; set; }

        public int? EmployeeCode { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string EmployeeDisplay =>
            EmployeeId.HasValue
                ? $"{EmployeeName} - {EmployeeCode}"
                : "Not linked";
        public bool RequirePasswordChange { get; set; }
    }

    public class UsuarioCreateDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        [Required]
        public int Lvl_Id { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        public Guid? EmployeeId { get; set; }
        public bool RequirePasswordChange { get; set; }
    }

    public class UsuarioUpdateDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; }

        public string Name { get; set; } = string.Empty;

        [Required]
        public int Lvl_Id { get; set; }

        [Required]
        public bool Activo { get; set; }

        public Guid? EmployeeId { get; set; }
        public bool RequirePasswordChange { get; set; }
    }

    public class PermisoComboDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class EmpleadoUsuarioComboDto
    {
        public Guid Id { get; set; }

        public int IdEmpleado { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string DisplayName => $"{Nombre} - {IdEmpleado}";
    }
}