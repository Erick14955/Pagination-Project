using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    public class Print_Foot
{
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }
        public string Print_Foot_Name { get; set; } = string.Empty;
    }
}
