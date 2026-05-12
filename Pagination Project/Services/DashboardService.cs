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

            await ActualizarFinalizadosPorDirxionAsync(db, today);

            DashboardStatsDto stats = new DashboardStatsDto
            {
                TotalUsers = await db.Users
                    .AsNoTracking()
                    .CountAsync(),

                TotalEmployees = await db.Empleados
                    .AsNoTracking()
                    .CountAsync(),

                TotalBooks = await db.Libros
                    .AsNoTracking()
                    .CountAsync(l => !l.Finalizado),

                TotalEvaluations = await db.Evaluaciones
                    .AsNoTracking()
                    .CountAsync(ev =>
                        !ev.Finalizado &&
                        ev.Asignacion != null &&
                        !ev.Asignacion.Finalizado &&
                        ev.Asignacion.Libro != null &&
                        !ev.Asignacion.Libro.Finalizado)
            };

            var data = await (
                from a in db.Asignaciones.AsNoTracking()
                join l in db.Libros.AsNoTracking() on a.IdLibro equals l.Id
                join e in db.Empleados.AsNoTracking() on a.IdEmpleado equals e.Id

                join bpJoin in db.Bind_Plants.AsNoTracking()
                    on l.BindPlantId equals bpJoin.Id into bindPlantGroup
                from bp in bindPlantGroup.DefaultIfEmpty()

                where !a.Finalizado &&
                      !l.Finalizado

                select new
                {
                    AssignmentId = a.Id,
                    BookId = l.Id,
                    EmployeeId = e.Id,

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
                    var bindPlantName = (x.BindPlantName ?? string.Empty)
                        .Trim()
                        .ToUpperInvariant();

                    var excluirMemoPorBindPlant =
                        bindPlantName == "PREM" ||
                        bindPlantName == "DIRX" ||
                        bindPlantName == "WSTR";

                    string stage = string.Empty;
                    DateOnly? stageDate = null;

                    var proofDisplayDate = GetNextBusinessDay(x.ProofExtract);
                    var finalDisplayDate = GetNextBusinessDay(x.FinalExtract);
                    var memoDisplayDate = GetNextBusinessDay(x.MemoExtract);

                    if (proofDisplayDate == today)
                    {
                        stage = "Proof Extract";
                        stageDate = proofDisplayDate;
                    }
                    else if (finalDisplayDate == today)
                    {
                        stage = "Final Extract";
                        stageDate = finalDisplayDate;
                    }
                    else if (memoDisplayDate == today && !excluirMemoPorBindPlant)
                    {
                        stage = "Memo Extract";
                        stageDate = memoDisplayDate;
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

        private static DateOnly GetNextBusinessDay(DateOnly sourceDate)
        {
            if (sourceDate == default)
                return default;

            var result = sourceDate.AddDays(1);

            while (result.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                result = result.AddDays(1);
            }

            return result;
        }

        private static async Task ActualizarFinalizadosPorDirxionAsync(AppDbContext db, DateOnly today)
        {
            var librosParaFinalizar = await db.Libros
                .Include(l => l.Asignaciones)
                    .ThenInclude(a => a.Evaluaciones)
                .Where(l =>
                    !l.Finalizado &&
                    l.DirxionDate < today)
                .ToListAsync();

            if (!librosParaFinalizar.Any())
                return;

            foreach (var libro in librosParaFinalizar)
            {
                libro.Finalizado = true;

                foreach (var asignacion in libro.Asignaciones)
                {
                    asignacion.Finalizado = true;

                    foreach (var evaluacion in asignacion.Evaluaciones)
                    {
                        evaluacion.Finalizado = true;
                    }
                }
            }

            await db.SaveChangesAsync();
        }
    }
}