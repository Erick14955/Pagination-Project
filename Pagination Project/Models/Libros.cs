using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace Pagination_Project.Models
{
    public class Libros
{
        public Guid Id { get; set; }
        public string PrintFooter { get; set; } = string.Empty;
        public string LegacyCoce { get; set; } = string.Empty;
        public string LSACode { get; set; } = string.Empty;
        public string KGENCode { get; set; } = string.Empty;
        public string BookName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string ProductIssue { get; set; } = string.Empty;
        public string TrimSize { get; set; } = string.Empty;
        public string BindPlant { get; set; } = string.Empty;
        public DateOnly ProofExtract { get; set; }
        public DateOnly FinalExtract { get; set; }
        public DateOnly MemoExtract { get; set; }
        public DateOnly FinalPODate { get; set; }
        public DateOnly ShippingDate { get; set; }
        public DateOnly DirxionDate { get; set; }
        public DateOnly PubDate { get; set; }
        public bool SRLSuppression { get; set; }
        public bool NWP { get; set; }
    }
}
