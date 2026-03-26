namespace Pagination_Project.Models
{
    public class AssignedBookDashboardDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string KgenCode { get; set; } = string.Empty;
    public string LsaCode { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public DateOnly? StageDate { get; set; }
}
}
