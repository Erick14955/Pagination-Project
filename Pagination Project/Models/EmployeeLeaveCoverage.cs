namespace Pagination_Project.Models
{
    public class EmployeeLeaveCoverage
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Empleados? Employee { get; set; }

        public string LeaveType { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public bool AutomaticCoverage { get; set; }

        public string? Notes { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public ICollection<EmployeeLeaveCoverageDetail> Details { get; set; } = new List<EmployeeLeaveCoverageDetail>();
    }
}