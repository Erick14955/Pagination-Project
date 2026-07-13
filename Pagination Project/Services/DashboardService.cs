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

            var trabajadasHoy = await db.AsignacionesTrabajadas
                .AsNoTracking()
                .Where(x => x.FechaTrabajo == today)
                .ToListAsync();

            var trabajadasPorAsignacion = trabajadasHoy
                .GroupBy(x => x.IdAsignacion)
                .ToDictionary(x => x.Key, x => x.First());

            var data = await (
                from a in db.Asignaciones.AsNoTracking()
                join l in db.Libros.AsNoTracking() on a.IdLibro equals l.Id
                join e in db.Empleados.AsNoTracking() on a.IdEmpleado equals e.Id
                join d in db.Databases.AsNoTracking() on l.DatabaseId equals d.Id into database
                from d in database.DefaultIfEmpty()

                join bpJoin in db.Bind_Plants.AsNoTracking()
                    on l.BindPlantId equals bpJoin.Id into bindPlantGroup
                from bp in bindPlantGroup.DefaultIfEmpty()

                where !a.Finalizado &&
                      !l.Finalizado

                select new RawAssignmentDashboardDto
                {
                    AssignmentId = a.Id,
                    BookId = l.Id,
                    EmployeeId = e.Id,

                    EmployeeName = e.Nombre ?? string.Empty,
                    KgenCode = l.KGENCode ?? string.Empty,
                    LsaCode = l.LSACode ?? string.Empty,
                    BookName = l.BookName ?? string.Empty,
                    Database = d != null ? d.Database_Name : string.Empty,

                    ProofExtract = l.ProofExtract,
                    FinalExtract = l.FinalExtract,
                    MemoExtract = l.MemoExtract,
                    DirxionDate = l.DirxionDate,
                    FinalPODate = l.FinalPODate,
                    ShippingDate = l.ShippingDate,

                    BindPlantName = bp != null ? bp.Bind_Plant_Name : string.Empty
                }
            ).ToListAsync();

            var assignmentIds = data
                .Select(x => x.AssignmentId)
                .Distinct()
                .ToList();

            var temporalesActivas = assignmentIds.Any()
                ? await db.TemporaryAssignments
                    .AsNoTracking()
                    .Include(x => x.TemporaryEmployee)
                    .Where(x => x.Active &&
                                assignmentIds.Contains(x.AssignmentId))
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync()
                : new List<TemporaryAssignment>();

            var assignedBooks = new List<AssignedBookDashboardDto>();
            var completedAssignments = new List<AssignedBookDashboardDto>();

            foreach (var item in data)
            {
                var stageInfo = ObtenerEtapaDelDia(item, today);

                if (stageInfo is null)
                    continue;

                trabajadasPorAsignacion.TryGetValue(item.AssignmentId, out var registroTrabajado);

                var etapaCompletada = EstaEtapaCompletada(registroTrabajado, stageInfo.StageKey);

                var temporalAplicable = temporalesActivas
                    .FirstOrDefault(x =>
                        x.AssignmentId == item.AssignmentId &&
                        AsignacionTemporalAplicaEtapa(x, stageInfo.StageKey));

                var employeeId = temporalAplicable?.TemporaryEmployeeId ?? item.EmployeeId;

                var employeeName = temporalAplicable?.TemporaryEmployee?.Nombre;

                if (string.IsNullOrWhiteSpace(employeeName))
                    employeeName = item.EmployeeName;

                var dto = new AssignedBookDashboardDto
                {
                    AssignmentId = item.AssignmentId,
                    BookId = item.BookId,
                    EmployeeId = employeeId,

                    EmployeeName = employeeName,
                    KgenCode = item.KgenCode,
                    LsaCode = item.LsaCode,
                    BookName = item.BookName,
                    Database = item.Database,

                    StageKey = stageInfo.StageKey,
                    Stage = stageInfo.StageName,
                    CompletionStatus = stageInfo.CompletionStatus,
                    StageDate = stageInfo.StageDate
                };

                if (etapaCompletada)
                    completedAssignments.Add(dto);
                else
                    assignedBooks.Add(dto);
            }

            assignedBooks = assignedBooks
                .OrderBy(x => x.StageDate)
                .ThenBy(x => x.EmployeeName)
                .ThenBy(x => x.BookName)
                .ToList();

            completedAssignments = completedAssignments
                .OrderBy(x => x.StageDate)
                .ThenBy(x => x.EmployeeName)
                .ThenBy(x => x.BookName)
                .ToList();

            var weeklyEvaluations = await CargarEvaluacionesSemanaAnteriorAsync(db, today);

            return new DashboardSummaryDto
            {
                Stats = stats,
                AssignedBooks = assignedBooks,
                CompletedAssignments = completedAssignments,
                WeeklyEvaluations = weeklyEvaluations
            };
        }

        private static bool AsignacionTemporalAplicaEtapa(
            TemporaryAssignment temporal,
            string stageKey)
        {
            return stageKey switch
            {
                "ProofExtract" => temporal.Proof,
                "FinalExtract" => temporal.Final,
                "MemoExtract" => temporal.Memo,
                "FinalPO" => temporal.FinalPO,
                "Shipping" => temporal.Shipping,
                "Dirxion" => temporal.Dirxion,
                _ => false
            };
        }

        private static async Task<List<WeeklyEvaluationDashboardDto>> CargarEvaluacionesSemanaAnteriorAsync(
            AppDbContext db,
            DateOnly today)
        {
            var rango = ObtenerRangoSemanaAnterior(today);

            var data = await (
                from ev in db.Evaluaciones.AsNoTracking()
                join a in db.Asignaciones.AsNoTracking() on ev.AssignationId equals a.Id
                join l in db.Libros.AsNoTracking() on a.IdLibro equals l.Id
                join e in db.Empleados.AsNoTracking() on a.IdEmpleado equals e.Id
                where l.ShippingDate >= rango.Start &&
                      l.ShippingDate <= rango.End
                select new RawWeeklyEvaluationDashboardDto
                {
                    EvaluationId = ev.Id,
                    AssignmentId = a.Id,
                    BookId = l.Id,
                    EmployeeId = e.Id,

                    EmployeeName = e.Nombre ?? string.Empty,
                    KgenCode = l.KGENCode ?? string.Empty,
                    LsaCode = l.LSACode ?? string.Empty,
                    BookName = l.BookName ?? string.Empty,
                    ShippingDate = l.ShippingDate,

                    MotifYp = ev.MotifYp,
                    MotifWp = ev.MotifWp,
                    InventoryReport = ev.InventoryReport,
                    ProductShippingFolder = ev.ProductShippingFolder,
                    TaskMemo = ev.TaskMemo,

                    TouchingRule = ev.TouchingRule,
                    PagesSwapped = ev.PagesSwapped,
                    PplpWrongPlace = ev.PplpWrongPlace,
                    CouponsHeading = ev.CouponsHeading,
                    DoubleTruckWrongPlace = ev.DoubleTruckWrongPlace,
                    FillersOutside = ev.FillersOutside,
                    MissingYspFiller = ev.MissingYspFiller,
                    GradeUnder75 = ev.GradeUnder75,

                    WhpsNoAnchors = ev.WhpsNoAnchors,
                    WfpsNoAnchors = ev.WfpsNoAnchors,
                    WdqcsNoAnchors = ev.WdqcsNoAnchors,

                    MissingCornerAd = ev.MissingCornerAd,
                    MissingBanner = ev.MissingBanner,
                    MissingRandomTab = ev.MissingRandomTab,
                    MissingForcedTab = ev.MissingForcedTab,

                    FileNamingIssue = ev.FileNamingIssue,
                    OutputWrongDate = ev.OutputWrongDate,
                    WrongPitstop = ev.WrongPitstop,
                    RestaurantBleedIssue = ev.RestaurantBleedIssue,
                    WrongSigFiller = ev.WrongSigFiller,
                    FobFolder = ev.FobFolder,
                    MissingPaidItem = ev.MissingPaidItem,
                    MissingSelfPromo = ev.MissingSelfPromo,

                    Corrections = ev.Corrections,
                    PendingCorrections = ev.PendingCorrections,
                    TaskMemoWrongComment = ev.TaskMemoWrongComment
                }
            ).ToListAsync();

            var assignmentIds = data
                .Select(x => x.AssignmentId)
                .Distinct()
                .ToList();

            var temporalesShipping = await db.TemporaryAssignments
                .AsNoTracking()
                .Include(x => x.TemporaryEmployee)
                .Where(x => x.Active &&
                            x.Shipping &&
                            assignmentIds.Contains(x.AssignmentId))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = new List<WeeklyEvaluationDashboardDto>();

            foreach (var item in data)
            {
                var temporalAplicable = temporalesShipping
                    .FirstOrDefault(x => x.AssignmentId == item.AssignmentId);

                var employeeId = temporalAplicable?.TemporaryEmployeeId ?? item.EmployeeId;

                var employeeName = temporalAplicable?.TemporaryEmployee?.Nombre;

                if (string.IsNullOrWhiteSpace(employeeName))
                    employeeName = item.EmployeeName;

                var motifYp = item.MotifYp ?? CalcularPorcentaje(
                    CountErrors(
                        item.TouchingRule,
                        item.PagesSwapped,
                        item.PplpWrongPlace,
                        item.CouponsHeading,
                        item.DoubleTruckWrongPlace,
                        item.FillersOutside,
                        item.MissingYspFiller,
                        item.GradeUnder75),
                    8);

                var motifWp = item.MotifWp ?? CalcularPorcentaje(
                    CountErrors(
                        item.WhpsNoAnchors,
                        item.WfpsNoAnchors,
                        item.WdqcsNoAnchors),
                    3);

                var inventory = item.InventoryReport ?? CalcularPorcentaje(
                    CountErrors(
                        item.MissingCornerAd,
                        item.MissingBanner,
                        item.MissingRandomTab,
                        item.MissingForcedTab),
                    4);

                var shipping = item.ProductShippingFolder ?? CalcularPorcentaje(
                    CountErrors(
                        item.FileNamingIssue,
                        item.OutputWrongDate,
                        item.WrongPitstop,
                        item.RestaurantBleedIssue,
                        item.WrongSigFiller,
                        item.FobFolder,
                        item.MissingPaidItem,
                        item.MissingSelfPromo),
                    8);

                var taskMemo = item.TaskMemo ?? CalcularPorcentaje(
                    CountErrors(
                        item.Corrections,
                        item.PendingCorrections,
                        item.TaskMemoWrongComment),
                    3);

                var totalAverage = CalcularPromedio(
                    motifYp,
                    motifWp,
                    inventory,
                    shipping,
                    taskMemo);

                result.Add(new WeeklyEvaluationDashboardDto
                {
                    EvaluationId = item.EvaluationId,
                    AssignmentId = item.AssignmentId,
                    BookId = item.BookId,
                    EmployeeId = employeeId,

                    EmployeeName = employeeName,
                    KgenCode = item.KgenCode,
                    LsaCode = item.LsaCode,
                    BookName = item.BookName,
                    ShippingDate = item.ShippingDate,

                    MotifYp = motifYp,
                    MotifWp = motifWp,
                    InventoryReport = inventory,
                    ProductShippingFolder = shipping,
                    TaskMemo = taskMemo,
                    PercentageAverage = totalAverage,

                    TouchingRule = item.TouchingRule,
                    PagesSwapped = item.PagesSwapped,
                    PplpWrongPlace = item.PplpWrongPlace,
                    CouponsHeading = item.CouponsHeading,
                    DoubleTruckWrongPlace = item.DoubleTruckWrongPlace,
                    FillersOutside = item.FillersOutside,
                    MissingYspFiller = item.MissingYspFiller,
                    GradeUnder75 = item.GradeUnder75,

                    WhpsNoAnchors = item.WhpsNoAnchors,
                    WfpsNoAnchors = item.WfpsNoAnchors,
                    WdqcsNoAnchors = item.WdqcsNoAnchors,

                    MissingCornerAd = item.MissingCornerAd,
                    MissingBanner = item.MissingBanner,
                    MissingRandomTab = item.MissingRandomTab,
                    MissingForcedTab = item.MissingForcedTab,

                    FileNamingIssue = item.FileNamingIssue,
                    OutputWrongDate = item.OutputWrongDate,
                    WrongPitstop = item.WrongPitstop,
                    RestaurantBleedIssue = item.RestaurantBleedIssue,
                    WrongSigFiller = item.WrongSigFiller,
                    FobFolder = item.FobFolder,
                    MissingPaidItem = item.MissingPaidItem,
                    MissingSelfPromo = item.MissingSelfPromo,

                    Corrections = item.Corrections,
                    PendingCorrections = item.PendingCorrections,
                    TaskMemoWrongComment = item.TaskMemoWrongComment
                });
            }

            return result
                .OrderBy(x => x.ShippingDate)
                .ThenBy(x => x.EmployeeName)
                .ThenBy(x => x.BookName)
                .ToList();
        }

        private static (DateOnly Start, DateOnly End) ObtenerRangoSemanaAnterior(DateOnly today)
        {
            var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

            var inicioSemanaActual = today.AddDays(-diff);
            var inicioSemanaAnterior = inicioSemanaActual.AddDays(-7);
            var finSemanaAnterior = inicioSemanaActual.AddDays(-1);

            return (inicioSemanaAnterior, finSemanaAnterior);
        }

        private static decimal CalcularPromedio(params decimal[] valores)
        {
            if (valores.Length == 0)
                return 0;

            return Math.Round(valores.Average(), 2);
        }

        private static decimal CalcularPorcentaje(int errores, int total)
        {
            if (total <= 0)
                return 0;

            var porcentaje = ((total - errores) * 100m) / total;

            if (porcentaje < 0)
                porcentaje = 0;

            return Math.Round(porcentaje, 2);
        }

        private static int CountErrors(params bool[] values)
        {
            return values.Count(x => x);
        }

        private static StageDashboardInfo? ObtenerEtapaDelDia(RawAssignmentDashboardDto item, DateOnly today)
        {
            var bindPlantName = (item.BindPlantName ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            var esIveau = EsBindPlant(bindPlantName, "IVEAU", "WSTR");
            var esAu = EsBindPlant(bindPlantName, "AU");
            var esNz = EsBindPlant(bindPlantName, "NZ");

            var excluirMemoPorBindPlant =
                EsBindPlant(bindPlantName, "PREM", "DIRX", "WSTR", "IVEAU", "AU", "NZ");

            var proofDisplayDate = GetNextBusinessDay(item.ProofExtract);
            var finalDisplayDate = GetNextBusinessDay(item.FinalExtract);
            var memoDisplayDate = GetNextBusinessDay(item.MemoExtract);

            var finalPODisplayDate = ObtenerFinalPODisplayDate(
                item,
                esIveau,
                esAu,
                esNz);

            var shippingDisplayDate = esIveau
                ? GetPreviousWorkDate(item.ShippingDate, 1)
                : item.ShippingDate;

            if (proofDisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey = "ProofExtract",
                    StageName = "Proof Extract",
                    CompletionStatus = "Proof Extract Completed",
                    StageDate = proofDisplayDate
                };
            }

            if (finalDisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey = "FinalExtract",
                    StageName = "Final Extract",
                    CompletionStatus = "Final Extract Completed",
                    StageDate = finalDisplayDate
                };
            }

            if (memoDisplayDate == today && !excluirMemoPorBindPlant)
            {
                return new StageDashboardInfo
                {
                    StageKey = "MemoExtract",
                    StageName = "Memo Extract",
                    CompletionStatus = "Memo Extract Completed",
                    StageDate = memoDisplayDate
                };
            }

            if (finalPODisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey = "FinalPO",
                    StageName = ObtenerNombreEtapaFinalPO(bindPlantName),
                    CompletionStatus = "Final PO Sent",
                    StageDate = finalPODisplayDate
                };
            }

            if (shippingDisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey = "Shipping",
                    StageName = "Shipping Date",
                    CompletionStatus = "Shipped Pages",
                    StageDate = shippingDisplayDate
                };
            }

            if (item.DirxionDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey = "Dirxion",
                    StageName = "Dirxion Date",
                    CompletionStatus = "Dirxion Sent",
                    StageDate = item.DirxionDate
                };
            }

            return null;
        }

        private static string ObtenerNombreEtapaFinalPO(string bindPlantName)
        {
            var bindPlantsPermitidos = new[]
            {
                "WAUK",
                "LMRA",
                "DIRX",
                "IVEAU",
                "SUSX",
                "PREM",
                "WSTR"
            };

            var bindPlantNormalizado = (bindPlantName ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            var bindPlantEncontrado = bindPlantsPermitidos.FirstOrDefault(codigo =>
                EsBindPlant(bindPlantNormalizado, codigo));

            if (!string.IsNullOrWhiteSpace(bindPlantEncontrado))
                return $"Final PO {bindPlantEncontrado}";

            return "Final PO Date";
        }

        private static DateOnly ObtenerFinalPODisplayDate(
            RawAssignmentDashboardDto item,
            bool esIveau,
            bool esAu,
            bool esNz)
        {
            if (esNz)
            {
                return GetSameOrNextBusinessDay(item.MemoExtract);
            }

            if (esAu)
            {
                if (item.MemoExtract == default)
                    return default;

                return GetSameOrNextBusinessDay(item.MemoExtract.AddDays(1));
            }

            if (esIveau)
            {
                return GetPreviousWorkDate(item.FinalPODate, 3);
            }

            return item.FinalPODate;
        }

        private static bool EsBindPlant(string bindPlantName, params string[] codigos)
        {
            if (string.IsNullOrWhiteSpace(bindPlantName))
                return false;

            var normalizado = bindPlantName.Trim().ToUpperInvariant();

            if (codigos.Any(c => normalizado == c.Trim().ToUpperInvariant()))
                return true;

            var partes = normalizado.Split(
                new[] { ' ', '-', '_', '/', '\\', ',', ';', '.', '|', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries);

            return partes.Any(parte =>
                codigos.Any(c => parte == c.Trim().ToUpperInvariant()));
        }

        private static DateOnly GetPreviousWorkDate(DateOnly sourceDate, int daysBefore)
        {
            if (sourceDate == default)
                return default;

            var result = sourceDate.AddDays(-daysBefore);

            while (result.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                result = result.AddDays(-1);
            }

            return result;
        }

        private static DateOnly GetSameOrNextBusinessDay(DateOnly sourceDate)
        {
            if (sourceDate == default)
                return default;

            var result = sourceDate;

            while (result.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                result = result.AddDays(1);
            }

            return result;
        }

        private static bool EstaEtapaCompletada(AsignacionTrabajada? registro, string stageKey)
        {
            if (registro is null)
                return false;

            return stageKey switch
            {
                "ProofExtract" => registro.ProofExtractWorked,
                "FinalExtract" => registro.FinalExtractWorked,
                "MemoExtract" => registro.MemoExtractWorked,
                "FinalPO" => registro.FinalPOWorked,
                "Shipping" => registro.ShippingWorked,
                "Dirxion" => registro.DirxionWorked,
                _ => false
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

        private class RawAssignmentDashboardDto
        {
            public Guid AssignmentId { get; set; }
            public Guid BookId { get; set; }
            public Guid EmployeeId { get; set; }

            public string EmployeeName { get; set; } = string.Empty;
            public string KgenCode { get; set; } = string.Empty;
            public string LsaCode { get; set; } = string.Empty;
            public string BookName { get; set; } = string.Empty;
            public string BindPlantName { get; set; } = string.Empty;
            public string Database { get; set; } = string.Empty;

            public DateOnly ProofExtract { get; set; }
            public DateOnly FinalExtract { get; set; }
            public DateOnly MemoExtract { get; set; }
            public DateOnly DirxionDate { get; set; }
            public DateOnly FinalPODate { get; set; }
            public DateOnly ShippingDate { get; set; }
        }

        private class RawWeeklyEvaluationDashboardDto
        {
            public Guid EvaluationId { get; set; }
            public Guid AssignmentId { get; set; }
            public Guid BookId { get; set; }
            public Guid EmployeeId { get; set; }

            public string EmployeeName { get; set; } = string.Empty;
            public string KgenCode { get; set; } = string.Empty;
            public string LsaCode { get; set; } = string.Empty;
            public string BookName { get; set; } = string.Empty;

            public DateOnly ShippingDate { get; set; }

            public decimal? MotifYp { get; set; }
            public decimal? MotifWp { get; set; }
            public decimal? InventoryReport { get; set; }
            public decimal? ProductShippingFolder { get; set; }
            public decimal? TaskMemo { get; set; }

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

        private class StageDashboardInfo
        {
            public string StageKey { get; set; } = string.Empty;
            public string StageName { get; set; } = string.Empty;
            public string CompletionStatus { get; set; } = string.Empty;
            public DateOnly StageDate { get; set; }
        }
    }
}