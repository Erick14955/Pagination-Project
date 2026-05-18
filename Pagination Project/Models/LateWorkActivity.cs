namespace Pagination_Project.Models
{
    public class LateWorkActivity
    {
        public Guid Id { get; set; }

        public Guid AssignmentId { get; set; }

        public long LateWorkId { get; set; }

        public long ClientId { get; set; }

        public bool Completed { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Asignaciones? Assignment { get; set; }
    }
}