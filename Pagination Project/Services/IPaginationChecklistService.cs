using System.Security.Claims;

namespace Pagination_Project.Services
{
    public interface IPaginationChecklistService
    {
        Task<PaginationChecklistDownloadResult> GeneratePaginationChecklistAsync(
            Guid bookId,
            Guid assignmentId,
            ClaimsPrincipal user);
    }

    public sealed class PaginationChecklistDownloadResult
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } =
            "application/vnd.ms-excel.sheet.macroEnabled.12";
    }
}