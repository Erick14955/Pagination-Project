namespace Pagination_Project.Models
{
    public class AssignedBookDashboardDto
    {
        public Guid AssignmentId { get; set; }

        public Guid BookId { get; set; }

        public Guid EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string KgenCode { get; set; } = string.Empty;

        public string LsaCode { get; set; } = string.Empty;

        public string BookName { get; set; } = string.Empty;

        public string Database { get; set; } = string.Empty;

        public short EmployeeTypeId { get; set; }

        public string EmployeeTypeCode { get; set; } = string.Empty;

        public string StageKey { get; set; } = string.Empty;

        public string Stage { get; set; } = string.Empty;

        public string CompletionStatus { get; set; } = string.Empty;

        public DateOnly? StageDate { get; set; }

        public bool BookReadyToShip { get; set; }
    }
}