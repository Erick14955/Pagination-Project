using Microsoft.AspNetCore.Components.Forms;
using Pagination_Project.Models;

namespace Pagination_Project.Services;

public interface IPsAlphabeticalCheckerService
{
    Task<PsBookAnalysisResult> AnalyzeBookAsync(
        IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default);
}
