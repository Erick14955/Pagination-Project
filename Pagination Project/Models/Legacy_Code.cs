using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    public class Legacy_Code
{
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }
        public string Legacy_Code_Name { get; set; } = string.Empty;
    }
}
