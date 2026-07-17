namespace Pagination_Project.Models
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalBooks { get; set; }
        public int TotalEvaluations { get; set; }
    }

    public class WeeklyEvaluationDashboardDto
    {
        public Guid EvaluationId { get; set; }
        public Guid AssignmentId { get; set; }
        public Guid BookId { get; set; }
        public Guid EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;
        public string KgenCode { get; set; } = string.Empty;
        public string LsaCode { get; set; } = string.Empty;
        public string BookName { get; set; } = string.Empty;
        public short EmployeeTypeId { get; set; }
        public string EmployeeTypeCode { get; set; } = string.Empty;

        public DateOnly ShippingDate { get; set; }

        public decimal MotifYp { get; set; }
        public decimal MotifWp { get; set; }
        public decimal InventoryReport { get; set; }
        public decimal ProductShippingFolder { get; set; }
        public decimal TaskMemo { get; set; }
        public decimal PercentageAverage { get; set; }

        public bool TouchingRule { get; set; }
        public bool PagesSwapped { get; set; }
        public bool PplpWrongPlace { get; set; }
        public bool CouponsHeading { get; set; }
        public bool DoubleTruckWrongPlace { get; set; }
        public bool FillersOutside { get; set; }
        public bool MissingYspFiller { get; set; }
        public bool GradeUnder75 { get; set; }

        public bool WhpsNoAnchors { get; set; }
        public bool WfpsNoAnchors { get; set; }
        public bool WdqcsNoAnchors { get; set; }

        public bool MissingCornerAd { get; set; }
        public bool MissingBanner { get; set; }
        public bool MissingRandomTab { get; set; }
        public bool MissingForcedTab { get; set; }

        public bool FileNamingIssue { get; set; }
        public bool OutputWrongDate { get; set; }
        public bool WrongPitstop { get; set; }
        public bool RestaurantBleedIssue { get; set; }
        public bool WrongSigFiller { get; set; }
        public bool FobFolder { get; set; }
        public bool MissingPaidItem { get; set; }
        public bool MissingSelfPromo { get; set; }

        public bool Corrections { get; set; }
        public bool PendingCorrections { get; set; }
        public bool TaskMemoWrongComment { get; set; }
    }
}
