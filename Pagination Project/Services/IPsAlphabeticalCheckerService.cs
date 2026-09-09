using Microsoft.AspNetCore.Components.Forms;
using Pagination_Project.Models;

namespace Pagination_Project.Services;

public enum PsAlphabeticalCheckMode
{
    Normal,
    SearchByHeader
}

public interface IPsAlphabeticalCheckerService
{
    Task<PsBookAnalysisResult> AnalyzeBookAsync(
        IReadOnlyList<IBrowserFile> files,
        PsAlphabeticalCheckMode mode = PsAlphabeticalCheckMode.Normal,
        CancellationToken cancellationToken = default);
}
