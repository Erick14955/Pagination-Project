using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public DashboardService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var yesterday = today.AddDays(-1);

            DashboardStatsDto stats = new DashboardStatsDto
            {
                TotalUsers = await db.Users.AsNoTracking().CountAsync(),
                TotalEmployees = await db.Empleados.AsNoTracking().CountAsync(),
                TotalBooks = await db.Libros.AsNoTracking().CountAsync(),
                TotalEvaluations = await db.Evaluaciones.AsNoTracking().CountAsync()
            };

            var assignedBooks = await (
                from a in db.Asignaciones.AsNoTracking()
                join l in db.Libros.AsNoTracking() on a.IdLibro equals l.Id
                join e in db.Empleados.AsNoTracking() on a.IdEmpleado equals e.Id
                where l.ProofExtract == yesterday
                   || l.FinalExtract == yesterday
                   || l.MemoExtract == yesterday
                   || l.DirxionDate == today
                   || l.FinalPODate == today
                   || l.ShippingDate == today
                select new AssignedBookDashboardDto
                {
                    EmployeeName = e.Nombre ?? string.Empty,
                    KgenCode = l.KGENCode ?? string.Empty,
                    LsaCode = l.LSACode ?? string.Empty,
                    BookName = l.BookName ?? string.Empty,
                    Stage =
                        l.ProofExtract == yesterday ? "Proof Extract" :
                        l.FinalExtract == yesterday ? "Final Extract" :
                        l.MemoExtract == yesterday ? "Memo Extract" :
                        l.DirxionDate == today ? "Dirxion Date" :
                        l.FinalPODate == today ? "Final PO Date" :
                        l.ShippingDate == today ? "Shipping Date" :
                        string.Empty,
                    StageDate =
                        l.ProofExtract == yesterday ? l.ProofExtract.AddDays(1) :
                        l.FinalExtract == yesterday ? l.FinalExtract.AddDays(1) :
                        l.MemoExtract == yesterday ? l.MemoExtract.AddDays(1) :
                        l.DirxionDate == today ? l.DirxionDate :
                        l.FinalPODate == today ? l.FinalPODate :
                        l.ShippingDate == today ? l.ShippingDate :
                        null
                }
            )
            .OrderBy(x => x.StageDate)
            .ThenBy(x => x.EmployeeName)
            .ToListAsync();

            return new DashboardSummaryDto
            {
                Stats = stats,
                AssignedBooks = assignedBooks
            };
        }
    }
}