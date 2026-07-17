using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(UserDataScope scope);

        Task<List<AssignedBookDashboardDto>> GetAssignedBooksForDateAsync(
            DateOnly targetDate,
            UserDataScope scope);
    }
}
