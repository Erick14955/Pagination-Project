using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pagination_Project.Services;

namespace Pagination_Project.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IUserDataScopeService _userDataScopeService;

        public DashboardController(
            IDashboardService dashboardService,
            IUserDataScopeService userDataScopeService)
        {
            _dashboardService = dashboardService;
            _userDataScopeService = userDataScopeService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var scope = await _userDataScopeService.GetScopeAsync(User);
            var result = await _dashboardService.GetDashboardSummaryAsync(scope);
            return Ok(result);
        }
    }
}