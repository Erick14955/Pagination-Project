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
    }

    public class PermisoComboDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}