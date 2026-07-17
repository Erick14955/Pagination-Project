using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        Task<List<AssignedBookDashboardDto>> GetAssignedBooksForDateAsync(DateOnly targetDate);
    }
}