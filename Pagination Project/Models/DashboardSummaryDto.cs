namespace Pagination_Project.Models
{
    public class DashboardSummaryDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<AssignedBookDashboardDto> AssignedBooks { get; set; } = new();
        public List<AssignedBookDashboardDto> CompletedAssignments { get; set; } = new();
        public List<WeeklyEvaluationDashboardDto> WeeklyEvaluations { get; set; } = new();
    }
}