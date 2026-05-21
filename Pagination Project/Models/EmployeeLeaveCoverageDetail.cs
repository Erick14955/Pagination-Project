namespace Pagination_Project.Models
{
    public class EmployeeLeaveCoverageDetail
    {
        public Guid Id { get; set; }

        public Guid EmployeeLeaveCoverageId { get; set; }
        public EmployeeLeaveCoverage? EmployeeLeaveCoverage { get; set; }

        public Guid AssignmentId { get; set; }
        public Asignaciones? Assignment { get; set; }

        public Guid? TemporaryAssignmentId { get; set; }
        public TemporaryAssignment? TemporaryAssignment { get; set; }

        public Guid OriginalEmployeeId { get; set; }
        public Guid TemporaryEmployeeId { get; set; }

        public Guid BookId { get; set; }

        public string? BookName { get; set; }
        public string? KGENCode { get; set; }
        public string? LSACode { get; set; }

        public string Stages { get; set; } = string.Empty;

        public DateOnly FirstStageDate { get; set; }
        public DateOnly LastStageDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}