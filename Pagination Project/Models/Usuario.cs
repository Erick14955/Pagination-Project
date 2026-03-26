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

        [Column("Lvl_Id")]
        public int lvl_Id { get; set; }

        [ForeignKey(nameof(lvl_Id))]
        public Permisos? Permisos { get; set; }
    }
}