using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    public class State
{
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }
        public string State_Name { get; set; } = string.Empty;
    }
}
