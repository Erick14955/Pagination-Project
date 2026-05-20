using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    [Table("Assignments_Worked")]
    public class AsignacionTrabajada
    {
        [Key]
        [Column("ID")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("Assignment_Id")]
        public Guid IdAsignacion { get; set; }

        [Column("Work_Date")]
        public DateOnly FechaTrabajo { get; set; }

        [Column("Proof_Extract_Worked")]
        public bool ProofExtractWorked { get; set; }

        [Column("Final_Extract_Worked")]
        public bool FinalExtractWorked { get; set; }

        [Column("Memo_Extract_Worked")]
        public bool MemoExtractWorked { get; set; }

        [Column("Final_PO_Worked")]
        public bool FinalPOWorked { get; set; }

        [Column("Shipping_Worked")]
        public bool ShippingWorked { get; set; }

        [Column("Dirxion_Worked")]
        public bool DirxionWorked { get; set; }

        public Asignaciones? Asignacion { get; set; }
    }
}