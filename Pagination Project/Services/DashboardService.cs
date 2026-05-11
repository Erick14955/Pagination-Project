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

            var data = await (
                from a in db.Asignaciones.AsNoTracking()
                join l in db.Libros.AsNoTracking() on a.IdLibro equals l.Id
                join e in db.Empleados.AsNoTracking() on a.IdEmpleado equals e.Id

                join bpJoin in db.Bind_Plants.AsNoTracking()
                    on l.BindPlantId equals bpJoin.Id into bindPlantGroup
                from bp in bindPlantGroup.DefaultIfEmpty()

                select new
                {
                    EmployeeName = e.Nombre ?? string.Empty,
                    KgenCode = l.KGENCode ?? string.Empty,
                    LsaCode = l.LSACode ?? string.Empty,
                    BookName = l.BookName ?? string.Empty,

                    l.ProofExtract,
                    l.FinalExtract,
                    l.MemoExtract,
                    l.DirxionDate,
                    l.FinalPODate,
                    l.ShippingDate,

                    BindPlantName = bp != null ? bp.Bind_Plant_Name : string.Empty
                }
            ).ToListAsync();

            var assignedBooks = data
                .Select(x =>
                {
                    var bindPlantName = (x.BindPlantName ?? string.Empty).Trim().ToUpperInvariant();

                    var excluirMemoPorBindPlant =
                        bindPlantName == "PREM" ||
                        bindPlantName == "DIRX";

                    string stage = string.Empty;
                    DateOnly? stageDate = null;

                    if (x.ProofExtract == yesterday)
                    {
                        stage = "Proof Extract";
                        stageDate = x.ProofExtract.AddDays(1);
                    }
                    else if (x.FinalExtract == yesterday)
                    {
                        stage = "Final Extract";
                        stageDate = x.FinalExtract.AddDays(1);
                    }
                    else if (x.MemoExtract == yesterday && !excluirMemoPorBindPlant)
                    {
                        stage = "Memo Extract";
                        stageDate = x.MemoExtract.AddDays(1);
                    }
                    else if (x.DirxionDate == today)
                    {
                        stage = "Dirxion Date";
                        stageDate = x.DirxionDate;
                    }
                    else if (x.FinalPODate == today)
                    {
                        stage = "Final PO Date";
                        stageDate = x.FinalPODate;
                    }
                    else if (x.ShippingDate == today)
                    {
                        stage = "Shipping Date";
                        stageDate = x.ShippingDate;
                    }

                    return new AssignedBookDashboardDto
                    {
                        EmployeeName = x.EmployeeName,
                        KgenCode = x.KgenCode,
                        LsaCode = x.LsaCode,
                        BookName = x.BookName,
                        Stage = stage,
                        StageDate = stageDate
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Stage))
                .OrderBy(x => x.StageDate)
                .ThenBy(x => x.EmployeeName)
                .ThenBy(x => x.BookName)
                .ToList();

            return new DashboardSummaryDto
            {
                Stats = stats,
                AssignedBooks = assignedBooks
            };
        }
    }
}