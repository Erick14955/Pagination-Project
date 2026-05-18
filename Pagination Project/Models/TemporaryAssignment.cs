using System;

namespace Pagination_Project.Models
{
    public class TemporaryAssignment
    {
        public Guid Id { get; set; }

        public Guid AssignmentId { get; set; }
        public Asignaciones? Assignment { get; set; }

        public Guid OriginalEmployeeId { get; set; }
        public Empleados? OriginalEmployee { get; set; }

        public Guid TemporaryEmployeeId { get; set; }
        public Empleados? TemporaryEmployee { get; set; }

        public bool Proof { get; set; }
        public bool Final { get; set; }
        public bool Memo { get; set; }
        public bool FinalPO { get; set; }
        public bool Shipping { get; set; }
        public bool Dirxion { get; set; }
        public bool PubDate { get; set; }

        public string? Reason { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ClosedAt { get; set; }
    }
}