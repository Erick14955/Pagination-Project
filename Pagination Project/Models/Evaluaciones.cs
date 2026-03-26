using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pagination_Project.Models
{
    public class Evaluaciones
    {
        public Guid Id { get; set; }

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

        public Guid AssignationId { get; set; }

        public decimal? MotifYp { get; set; }
        public decimal? MotifWp { get; set; }
        public decimal? InventoryReport { get; set; }
        public decimal? ProductShippingFolder { get; set; }
        public decimal? TaskMemo { get; set; }

        public Asignaciones Asignacion { get; set; }
    }
}
