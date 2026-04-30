using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    public class Trim_Size
{
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }
        public string Trim_Size_Name { get; set; } = string.Empty;
    }
}
