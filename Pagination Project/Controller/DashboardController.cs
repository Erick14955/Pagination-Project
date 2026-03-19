using Microsoft.AspNetCore.Mvc;
using Pagination_Project.Services;

namespace Pagination_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly SupabaseDashboardService _dashboardService;

        public DashboardController(SupabaseDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error interno cargando estadísticas.",
                    detail = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
    }
}