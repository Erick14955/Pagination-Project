using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pagination_Project.Services;

namespace Pagination_Project.Controllers
{
    [Authorize]
    [Route("api/checklists")]
    [ApiController]
    public class ChecklistController : ControllerBase
    {
        private readonly IPaginationChecklistService _checklistService;

        public ChecklistController(IPaginationChecklistService checklistService)
        {
            _checklistService = checklistService;
        }

        [HttpGet("pagination/{bookId:guid}")]
        public async Task<IActionResult> DownloadPaginationChecklist(
            Guid bookId,
            [FromQuery] Guid assignmentId)
        {
            try
            {
                var result = await _checklistService.GeneratePaginationChecklistAsync(
                    bookId,
                    assignmentId,
                    User);

                return File(
                    result.Content,
                    result.ContentType,
                    result.FileName);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}