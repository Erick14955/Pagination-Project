using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    public class Bind_Plant
{
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }
        public string Bind_Plant_Name { get; set; } = string.Empty;
    }
}
