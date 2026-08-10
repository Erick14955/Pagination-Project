using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;

namespace Pagination_Project.Services
{
    public class DashboardService : IDashboardService
    {
        private const int CandidateDaysBefore = 3;
        private const int CandidateDaysAfter = 5;

        private static readonly char[] BindPlantSeparators =
        {
            ' ',
            '-',
            '_',
            '/',
            '\\',
            ',',
            ';',
            '.',
            '|',
            '(',
            ')',
            '[',
            ']'
        };

        private static readonly string[] NamedFinalPoBindPlants =
        {
            "WAUK",
            "LMRA",
            "DIRX",
            "IVEAU",
            "SUSX",
            "PREM",
            "WSTR"
        };

        private static readonly TimeZoneInfo DominicanTimeZone =
            ResolveDominicanTimeZone();

        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public DashboardService(
            IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
            UserDataScope scope)
        {
            ValidarScope(scope);

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var today =
                ObtenerFechaHoyRepublicaDominicana();

            await ActualizarFinalizadosPorDirxionAsync(
                db,
                today,
                scope);

            var stats =
                await CargarEstadisticasAsync(
                    db,
                    scope);

            var asignacionesHoy =
                await CargarAsignacionesPorFechaAsync(
                    db,
                    today,
                    scope);

            var weeklyEvaluations =
                await CargarEvaluacionesSemanaAnteriorAsync(
                    db,
                    today,
                    scope);

            return new DashboardSummaryDto
            {
                Stats = stats,

                AssignedBooks =
                    asignacionesHoy.Pending,

                CompletedAssignments =
                    asignacionesHoy.Completed,

                WeeklyEvaluations =
                    weeklyEvaluations
            };
        }

        public async Task<List<AssignedBookDashboardDto>>
            GetAssignedBooksForDateAsync(
                DateOnly targetDate,
                UserDataScope scope)
        {
            ValidarScope(scope);

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var today =
                ObtenerFechaHoyRepublicaDominicana();

            await ActualizarFinalizadosPorDirxionAsync(
                db,
                today,
                scope);

            var result =
                await CargarAsignacionesPorFechaAsync(
                    db,
                    targetDate,
                    scope);

            return result.Pending;
        }

        private static async Task<DashboardStatsDto>
            CargarEstadisticasAsync(
                AppDbContext db,
                UserDataScope scope)
        {
            IQueryable<Usuario> usersQuery =
                db.Users.AsNoTracking();

            if (!scope.ViewAllEmployeeTypes)
            {
                var employeeTypeId =
                    scope.EmployeeTypeId!.Value;

                usersQuery =
                    usersQuery.Where(u =>
                        u.Empleado != null &&
                        u.Empleado.EmployeeTypeId ==
                        employeeTypeId);
            }

            var totalUsers =
                await usersQuery.CountAsync();

            var totalEmployees =
                await db.Empleados
                    .AsNoTracking()
                    .ApplyScope(scope)
                    .CountAsync();

            var totalBooks =
                await db.Libros
                    .AsNoTracking()
                    .ApplyScope(scope)
                    .CountAsync(l =>
                        !l.Finalizado);

            var totalEvaluations =
                await db.Evaluaciones
                    .AsNoTracking()
                    .ApplyScope(scope)
                    .CountAsync(ev =>
                        !ev.Finalizado &&
                        !ev.Asignacion!.Finalizado &&
                        !ev.Asignacion.Libro!.Finalizado);

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                TotalEmployees = totalEmployees,
                TotalBooks = totalBooks,
                TotalEvaluations = totalEvaluations
            };
        }

        private static async Task<(
            List<AssignedBookDashboardDto> Pending,
            List<AssignedBookDashboardDto> Completed)>
            CargarAsignacionesPorFechaAsync(
                AppDbContext db,
                DateOnly targetDate,
                UserDataScope scope)
        {
            var candidateStartDate =
                targetDate.AddDays(
                    -CandidateDaysBefore);

            var candidateEndDate =
                targetDate.AddDays(
                    CandidateDaysAfter);

            var data = await (
                from a in db.Asignaciones.AsNoTracking()

                join l in db.Libros.AsNoTracking()
                    on a.IdLibro equals l.Id

                join e in db.Empleados.AsNoTracking()
                    on a.IdEmpleado equals e.Id

                join et in db.EmployeeTypes.AsNoTracking()
                    on l.EmployeeTypeId equals et.Id

                join d in db.Databases.AsNoTracking()
                    on l.DatabaseId equals d.Id
                    into databaseGroup

                from d in databaseGroup.DefaultIfEmpty()

                join bpJoin in db.Bind_Plants.AsNoTracking()
                    on l.BindPlantId equals bpJoin.Id
                    into bindPlantGroup

                from bp in bindPlantGroup.DefaultIfEmpty()

                where
                    (
                        (
                            l.ProofExtract >= candidateStartDate &&
                            l.ProofExtract <= candidateEndDate
                        )
                        ||
                        (
                            l.FinalExtract >= candidateStartDate &&
                            l.FinalExtract <= candidateEndDate
                        )
                        ||
                        (
                            l.MemoExtract >= candidateStartDate &&
                            l.MemoExtract <= candidateEndDate
                        )
                        ||
                        (
                            l.FinalPODate >= candidateStartDate &&
                            l.FinalPODate <= candidateEndDate
                        )
                        ||
                        (
                            l.ShippingDate >= candidateStartDate &&
                            l.ShippingDate <= candidateEndDate
                        )
                        ||
                        (
                            l.DirxionDate >= candidateStartDate &&
                            l.DirxionDate <= candidateEndDate
                        )
                    )
                    &&
                    (
                        (!a.Finalizado && !l.Finalizado)
                        ||
                        db.AsignacionesTrabajadas.Any(
                            aw =>
                                aw.IdAsignacion == a.Id)
                    )
                    &&
                    (
                        scope.ViewAllEmployeeTypes
                        ||
                        l.EmployeeTypeId ==
                        scope.EmployeeTypeId
                    )

                select new RawAssignmentDashboardDto
                {
                    AssignmentId = a.Id,
                    BookId = l.Id,
                    EmployeeId = e.Id,

                    EmployeeName =
                        e.Nombre ?? string.Empty,

                    KgenCode =
                        l.KGENCode ?? string.Empty,

                    LsaCode =
                        l.LSACode ?? string.Empty,

                    BookName =
                        l.BookName ?? string.Empty,

                    Database =
                        d != null
                            ? d.Database_Name
                            : string.Empty,

                    EmployeeTypeId =
                        l.EmployeeTypeId,

                    EmployeeTypeCode =
                        et.Code ?? string.Empty,

                    ProofExtract =
                        l.ProofExtract,

                    FinalExtract =
                        l.FinalExtract,

                    MemoExtract =
                        l.MemoExtract,

                    DirxionDate =
                        l.DirxionDate,

                    FinalPODate =
                        l.FinalPODate,

                    ShippingDate =
                        l.ShippingDate,

                    BindPlantName =
                        bp != null
                            ? bp.Bind_Plant_Name ?? string.Empty
                            : string.Empty
                }
            ).ToListAsync();

            if (data.Count == 0)
            {
                return (
                    new List<AssignedBookDashboardDto>(),
                    new List<AssignedBookDashboardDto>());
            }

            var assignmentIds =
                data
                    .Select(x => x.AssignmentId)
                    .Distinct()
                    .ToList();

            var registrosTrabajados =
                await db.AsignacionesTrabajadas
                    .AsNoTracking()
                    .Where(x =>
                        assignmentIds.Contains(
                            x.IdAsignacion))
                    .OrderByDescending(x =>
                        x.FechaTrabajo)
                    .Select(x =>
                        new RawAssignmentWorkDto
                        {
                            AssignmentId =
                                x.IdAsignacion,

                            ProofExtractWorked =
                                x.ProofExtractWorked,

                            FinalExtractWorked =
                                x.FinalExtractWorked,

                            MemoExtractWorked =
                                x.MemoExtractWorked,

                            FinalPOWorked =
                                x.FinalPOWorked,

                            ShippingWorked =
                                x.ShippingWorked,

                            DirxionWorked =
                                x.DirxionWorked,

                            ClosingCorrectionsVerified =
                                x.ClosingCorrectionsVerified,

                            LateWorkVerified =
                                x.LateWorkVerified
                        })
                    .ToListAsync();

            var workStateByAssignment =
                ConstruirEstadosTrabajo(
                    registrosTrabajados);

            var temporalesActivos =
                await CargarTemporalesActivosAsync(
                    db,
                    assignmentIds);

            var temporalByAssignmentStage =
                ConstruirTemporalesPorEtapa(
                    temporalesActivos);

            var pending =
                new List<AssignedBookDashboardDto>(
                    data.Count);

            var completed =
                new List<AssignedBookDashboardDto>(
                    data.Count);

            foreach (var item in data)
            {
                var bindPlantCodes =
                    ObtenerCodigosBindPlant(
                        item.BindPlantName);

                var stageInfo =
                    ObtenerEtapaDelDia(
                        item,
                        targetDate,
                        bindPlantCodes);

                if (stageInfo is null)
                {
                    continue;
                }

                workStateByAssignment.TryGetValue(
                    item.AssignmentId,
                    out var workState);

                var etapaCompletada =
                    EstaEtapaCompletada(
                        workState,
                        stageInfo.StageKey);

                var bookReadyToShip =
                    EstaListoParaShipping(
                        bindPlantCodes,
                        workState);

                temporalByAssignmentStage.TryGetValue(
                    (
                        item.AssignmentId,
                        stageInfo.StageKey
                    ),
                    out var temporalAplicable);

                var employeeId =
                    temporalAplicable?
                        .TemporaryEmployeeId
                    ?? item.EmployeeId;

                var employeeName =
                    temporalAplicable?
                        .TemporaryEmployeeName;

                if (string.IsNullOrWhiteSpace(
                        employeeName))
                {
                    employeeName =
                        item.EmployeeName;
                }

                var dto =
                    new AssignedBookDashboardDto
                    {
                        AssignmentId =
                            item.AssignmentId,

                        BookId =
                            item.BookId,

                        EmployeeId =
                            employeeId,

                        EmployeeName =
                            employeeName ?? string.Empty,

                        KgenCode =
                            item.KgenCode,

                        LsaCode =
                            item.LsaCode,

                        BookName =
                            item.BookName,

                        Database =
                            item.Database,

                        EmployeeTypeId =
                            item.EmployeeTypeId,

                        EmployeeTypeCode =
                            item.EmployeeTypeCode,

                        StageKey =
                            stageInfo.StageKey,

                        Stage =
                            stageInfo.StageName,

                        CompletionStatus =
                            stageInfo.CompletionStatus,

                        StageDate =
                            stageInfo.StageDate,

                        BookReadyToShip =
                            bookReadyToShip
                    };

                if (etapaCompletada)
                {
                    completed.Add(dto);
                }
                else
                {
                    pending.Add(dto);
                }
            }

            pending.Sort(
                AssignedBookDashboardComparer.Instance);

            completed.Sort(
                AssignedBookDashboardComparer.Instance);

            return (
                pending,
                completed);
        }

        private static Dictionary<Guid, AssignmentWorkState>
            ConstruirEstadosTrabajo(
                IEnumerable<RawAssignmentWorkDto> registros)
        {
            var result =
                new Dictionary<Guid, AssignmentWorkState>();

            foreach (var registro in registros)
            {
                if (!result.TryGetValue(
                        registro.AssignmentId,
                        out var state))
                {
                    state =
                        new AssignmentWorkState();

                    result.Add(
                        registro.AssignmentId,
                        state);
                }

                state.ProofExtractWorked |=
                    registro.ProofExtractWorked;

                state.FinalExtractWorked |=
                    registro.FinalExtractWorked;

                state.MemoExtractWorked |=
                    registro.MemoExtractWorked;

                state.FinalPOWorked |=
                    registro.FinalPOWorked;

                state.ShippingWorked |=
                    registro.ShippingWorked;

                state.DirxionWorked |=
                    registro.DirxionWorked;

                if (registro.FinalPOWorked &&
                    !state.HasFinalPoVerification)
                {
                    state.HasFinalPoVerification =
                        true;

                    state.ClosingCorrectionsVerified =
                        registro.ClosingCorrectionsVerified;

                    state.LateWorkVerified =
                        registro.LateWorkVerified;
                }
            }

            return result;
        }

        private static async Task<List<RawTemporaryAssignmentDto>>
            CargarTemporalesActivosAsync(
                AppDbContext db,
                IReadOnlyCollection<Guid> assignmentIds,
                bool soloShipping = false)
        {
            if (assignmentIds.Count == 0)
            {
                return new List<RawTemporaryAssignmentDto>();
            }

            var query =
                db.TemporaryAssignments
                    .AsNoTracking()
                    .Where(x =>
                        x.Active &&
                        assignmentIds.Contains(
                            x.AssignmentId));

            if (soloShipping)
            {
                query =
                    query.Where(x =>
                        x.Shipping);
            }

            return await query
                .OrderByDescending(x =>
                    x.CreatedAt)
                .Select(x =>
                    new RawTemporaryAssignmentDto
                    {
                        AssignmentId =
                            x.AssignmentId,

                        TemporaryEmployeeId =
                            x.TemporaryEmployeeId,

                        TemporaryEmployeeName =
                            x.TemporaryEmployee != null
                                ? x.TemporaryEmployee.Nombre
                                    ?? string.Empty
                                : string.Empty,

                        Proof =
                            x.Proof,

                        Final =
                            x.Final,

                        Memo =
                            x.Memo,

                        FinalPO =
                            x.FinalPO,

                        Shipping =
                            x.Shipping,

                        Dirxion =
                            x.Dirxion
                    })
                .ToListAsync();
        }

        private static Dictionary<
                (Guid AssignmentId, string StageKey),
                RawTemporaryAssignmentDto>
            ConstruirTemporalesPorEtapa(
                IEnumerable<RawTemporaryAssignmentDto> temporales)
        {
            var result =
                new Dictionary<
                    (Guid AssignmentId, string StageKey),
                    RawTemporaryAssignmentDto>();

            foreach (var temporal in temporales)
            {
                if (temporal.Proof)
                {
                    result.TryAdd(
                        (
                            temporal.AssignmentId,
                            "ProofExtract"
                        ),
                        temporal);
                }

                if (temporal.Final)
                {
                    result.TryAdd(
                        (
                            temporal.AssignmentId,
                            "FinalExtract"
                        ),
                        temporal);
                }

                if (temporal.Memo)
                {
                    result.TryAdd(
                        (
                            temporal.AssignmentId,
                            "MemoExtract"
                        ),
                        temporal);
                }

                if (temporal.FinalPO)
                {
                    result.TryAdd(
                        (
                            temporal.AssignmentId,
                            "FinalPO"
                        ),
                        temporal);
                }

                if (temporal.Shipping)
                {
                    result.TryAdd(
                        (
                            temporal.AssignmentId,
                            "Shipping"
                        ),
                        temporal);
                }

                if (temporal.Dirxion)
                {
                    result.TryAdd(
                        (
                            temporal.AssignmentId,
                            "Dirxion"
                        ),
                        temporal);
                }
            }

            return result;
        }

        private static async Task<List<WeeklyEvaluationDashboardDto>>
            CargarEvaluacionesSemanaAnteriorAsync(
                AppDbContext db,
                DateOnly today,
                UserDataScope scope)
        {
            var rango =
                ObtenerRangoSemanaAnterior(
                    today);

            var data = await (
                from ev in db.Evaluaciones.AsNoTracking()

                join a in db.Asignaciones.AsNoTracking()
                    on ev.AssignationId equals a.Id

                join l in db.Libros.AsNoTracking()
                    on a.IdLibro equals l.Id

                join e in db.Empleados.AsNoTracking()
                    on a.IdEmpleado equals e.Id

                join et in db.EmployeeTypes.AsNoTracking()
                    on l.EmployeeTypeId equals et.Id

                where
                    l.ShippingDate >= rango.Start
                    &&
                    l.ShippingDate <= rango.End
                    &&
                    (
                        scope.ViewAllEmployeeTypes
                        ||
                        l.EmployeeTypeId ==
                        scope.EmployeeTypeId
                    )

                select new RawWeeklyEvaluationDashboardDto
                {
                    EvaluationId = ev.Id,
                    AssignmentId = a.Id,
                    BookId = l.Id,
                    EmployeeId = e.Id,

                    EmployeeName =
                        e.Nombre ?? string.Empty,

                    KgenCode =
                        l.KGENCode ?? string.Empty,

                    LsaCode =
                        l.LSACode ?? string.Empty,

                    BookName =
                        l.BookName ?? string.Empty,

                    EmployeeTypeId =
                        l.EmployeeTypeId,

                    EmployeeTypeCode =
                        et.Code ?? string.Empty,

                    ShippingDate =
                        l.ShippingDate,

                    MotifYp =
                        ev.MotifYp,

                    MotifWp =
                        ev.MotifWp,

                    InventoryReport =
                        ev.InventoryReport,

                    ProductShippingFolder =
                        ev.ProductShippingFolder,

                    TaskMemo =
                        ev.TaskMemo,

                    TouchingRule =
                        ev.TouchingRule,

                    PagesSwapped =
                        ev.PagesSwapped,

                    PplpWrongPlace =
                        ev.PplpWrongPlace,

                    CouponsHeading =
                        ev.CouponsHeading,

                    DoubleTruckWrongPlace =
                        ev.DoubleTruckWrongPlace,

                    FillersOutside =
                        ev.FillersOutside,

                    MissingYspFiller =
                        ev.MissingYspFiller,

                    GradeUnder75 =
                        ev.GradeUnder75,

                    WhpsNoAnchors =
                        ev.WhpsNoAnchors,

                    WfpsNoAnchors =
                        ev.WfpsNoAnchors,

                    WdqcsNoAnchors =
                        ev.WdqcsNoAnchors,

                    MissingCornerAd =
                        ev.MissingCornerAd,

                    MissingBanner =
                        ev.MissingBanner,

                    MissingRandomTab =
                        ev.MissingRandomTab,

                    MissingForcedTab =
                        ev.MissingForcedTab,

                    FileNamingIssue =
                        ev.FileNamingIssue,

                    OutputWrongDate =
                        ev.OutputWrongDate,

                    WrongPitstop =
                        ev.WrongPitstop,

                    RestaurantBleedIssue =
                        ev.RestaurantBleedIssue,

                    WrongSigFiller =
                        ev.WrongSigFiller,

                    FobFolder =
                        ev.FobFolder,

                    MissingPaidItem =
                        ev.MissingPaidItem,

                    MissingSelfPromo =
                        ev.MissingSelfPromo,

                    Corrections =
                        ev.Corrections,

                    PendingCorrections =
                        ev.PendingCorrections,

                    TaskMemoWrongComment =
                        ev.TaskMemoWrongComment
                }
            ).ToListAsync();

            if (data.Count == 0)
            {
                return new List<WeeklyEvaluationDashboardDto>();
            }

            var assignmentIds =
                data
                    .Select(x =>
                        x.AssignmentId)
                    .Distinct()
                    .ToList();

            var temporalesShipping =
                await CargarTemporalesActivosAsync(
                    db,
                    assignmentIds,
                    soloShipping: true);

            var temporalesPorEtapa =
                ConstruirTemporalesPorEtapa(
                    temporalesShipping);

            var result =
                new List<WeeklyEvaluationDashboardDto>(
                    data.Count);

            foreach (var item in data)
            {
                temporalesPorEtapa.TryGetValue(
                    (
                        item.AssignmentId,
                        "Shipping"
                    ),
                    out var temporalAplicable);

                var employeeId =
                    temporalAplicable?
                        .TemporaryEmployeeId
                    ?? item.EmployeeId;

                var employeeName =
                    temporalAplicable?
                        .TemporaryEmployeeName;

                if (string.IsNullOrWhiteSpace(
                        employeeName))
                {
                    employeeName =
                        item.EmployeeName;
                }

                var motifYp =
                    item.MotifYp
                    ??
                    CalcularPorcentaje(
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

                var motifWp =
                    item.MotifWp
                    ??
                    CalcularPorcentaje(
                        CountErrors(
                            item.WhpsNoAnchors,
                            item.WfpsNoAnchors,
                            item.WdqcsNoAnchors),
                        3);

                var inventory =
                    item.InventoryReport
                    ??
                    CalcularPorcentaje(
                        CountErrors(
                            item.MissingCornerAd,
                            item.MissingBanner,
                            item.MissingRandomTab,
                            item.MissingForcedTab),
                        4);

                var shipping =
                    item.ProductShippingFolder
                    ??
                    CalcularPorcentaje(
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

                var taskMemo =
                    item.TaskMemo
                    ??
                    CalcularPorcentaje(
                        CountErrors(
                            item.Corrections,
                            item.PendingCorrections,
                            item.TaskMemoWrongComment),
                        3);

                var totalAverage =
                    CalcularPromedio(
                        motifYp,
                        motifWp,
                        inventory,
                        shipping,
                        taskMemo);

                result.Add(
                    new WeeklyEvaluationDashboardDto
                    {
                        EvaluationId =
                            item.EvaluationId,

                        AssignmentId =
                            item.AssignmentId,

                        BookId =
                            item.BookId,

                        EmployeeId =
                            employeeId,

                        EmployeeName =
                            employeeName ?? string.Empty,

                        KgenCode =
                            item.KgenCode,

                        LsaCode =
                            item.LsaCode,

                        BookName =
                            item.BookName,

                        EmployeeTypeId =
                            item.EmployeeTypeId,

                        EmployeeTypeCode =
                            item.EmployeeTypeCode,

                        ShippingDate =
                            item.ShippingDate,

                        MotifYp =
                            motifYp,

                        MotifWp =
                            motifWp,

                        InventoryReport =
                            inventory,

                        ProductShippingFolder =
                            shipping,

                        TaskMemo =
                            taskMemo,

                        PercentageAverage =
                            totalAverage,

                        TouchingRule =
                            item.TouchingRule,

                        PagesSwapped =
                            item.PagesSwapped,

                        PplpWrongPlace =
                            item.PplpWrongPlace,

                        CouponsHeading =
                            item.CouponsHeading,

                        DoubleTruckWrongPlace =
                            item.DoubleTruckWrongPlace,

                        FillersOutside =
                            item.FillersOutside,

                        MissingYspFiller =
                            item.MissingYspFiller,

                        GradeUnder75 =
                            item.GradeUnder75,

                        WhpsNoAnchors =
                            item.WhpsNoAnchors,

                        WfpsNoAnchors =
                            item.WfpsNoAnchors,

                        WdqcsNoAnchors =
                            item.WdqcsNoAnchors,

                        MissingCornerAd =
                            item.MissingCornerAd,

                        MissingBanner =
                            item.MissingBanner,

                        MissingRandomTab =
                            item.MissingRandomTab,

                        MissingForcedTab =
                            item.MissingForcedTab,

                        FileNamingIssue =
                            item.FileNamingIssue,

                        OutputWrongDate =
                            item.OutputWrongDate,

                        WrongPitstop =
                            item.WrongPitstop,

                        RestaurantBleedIssue =
                            item.RestaurantBleedIssue,

                        WrongSigFiller =
                            item.WrongSigFiller,

                        FobFolder =
                            item.FobFolder,

                        MissingPaidItem =
                            item.MissingPaidItem,

                        MissingSelfPromo =
                            item.MissingSelfPromo,

                        Corrections =
                            item.Corrections,

                        PendingCorrections =
                            item.PendingCorrections,

                        TaskMemoWrongComment =
                            item.TaskMemoWrongComment
                    });
            }

            result.Sort(
                WeeklyEvaluationDashboardComparer.Instance);

            return result;
        }

        private static (
            DateOnly Start,
            DateOnly End)
            ObtenerRangoSemanaAnterior(
                DateOnly today)
        {
            var diff =
                (
                    (int)today.DayOfWeek -
                    (int)DayOfWeek.Monday +
                    7
                ) % 7;

            var inicioSemanaActual =
                today.AddDays(
                    -diff);

            return (
                inicioSemanaActual.AddDays(-7),
                inicioSemanaActual.AddDays(-1)
            );
        }

        private static int CountErrors(
            bool value1,
            bool value2,
            bool value3)
        {
            return
                (value1 ? 1 : 0) +
                (value2 ? 1 : 0) +
                (value3 ? 1 : 0);
        }

        private static int CountErrors(
            bool value1,
            bool value2,
            bool value3,
            bool value4)
        {
            return
                (value1 ? 1 : 0) +
                (value2 ? 1 : 0) +
                (value3 ? 1 : 0) +
                (value4 ? 1 : 0);
        }

        private static int CountErrors(
            bool value1,
            bool value2,
            bool value3,
            bool value4,
            bool value5,
            bool value6,
            bool value7,
            bool value8)
        {
            return
                (value1 ? 1 : 0) +
                (value2 ? 1 : 0) +
                (value3 ? 1 : 0) +
                (value4 ? 1 : 0) +
                (value5 ? 1 : 0) +
                (value6 ? 1 : 0) +
                (value7 ? 1 : 0) +
                (value8 ? 1 : 0);
        }

        private static decimal CalcularPromedio(
            decimal value1,
            decimal value2,
            decimal value3,
            decimal value4,
            decimal value5)
        {
            return Math.Round(
                (
                    value1 +
                    value2 +
                    value3 +
                    value4 +
                    value5
                ) / 5m,
                2);
        }

        private static decimal CalcularPorcentaje(
            int errores,
            int total)
        {
            if (total <= 0)
            {
                return 0;
            }

            var porcentaje =
                (
                    (total - errores) *
                    100m
                ) / total;

            if (porcentaje < 0)
            {
                porcentaje = 0;
            }

            return Math.Round(
                porcentaje,
                2);
        }

        private static StageDashboardInfo? ObtenerEtapaDelDia(
            RawAssignmentDashboardDto item,
            DateOnly today,
            HashSet<string> bindPlantCodes)
        {
            var esIveau =
                bindPlantCodes.Contains("IVEAU") ||
                bindPlantCodes.Contains("WSTR");

            var esAu =
                bindPlantCodes.Contains("AU");

            var esNz =
                bindPlantCodes.Contains("NZ");

            var esNzShipping =
                bindPlantCodes.Contains("WSTR") ||
                bindPlantCodes.Contains("NZ");

            var excluirMemoPorBindPlant =
                bindPlantCodes.Contains("PREM") ||
                bindPlantCodes.Contains("DIRX") ||
                bindPlantCodes.Contains("WSTR") ||
                bindPlantCodes.Contains("IVEAU") ||
                bindPlantCodes.Contains("AU") ||
                bindPlantCodes.Contains("NZ");

            var proofDisplayDate =
                GetNextBusinessDay(
                    item.ProofExtract);

            var finalDisplayDate =
                GetNextBusinessDay(
                    item.FinalExtract);

            var memoDisplayDate =
                GetNextBusinessDay(
                    item.MemoExtract);

            var finalPODisplayDate =
                ObtenerFinalPODisplayDate(
                    item,
                    esIveau,
                    esAu,
                    esNz);

            var shippingDisplayDate =
                esNzShipping
                    ? GetPreviousWorkDate(
                        item.ShippingDate,
                        1)
                    : item.ShippingDate;

            if (proofDisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey =
                        "ProofExtract",

                    StageName =
                        "Proof Extract",

                    CompletionStatus =
                        "Proof Extract Completed",

                    StageDate =
                        proofDisplayDate
                };
            }

            if (finalDisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey =
                        "FinalExtract",

                    StageName =
                        "Final Extract",

                    CompletionStatus =
                        "Final Extract Completed",

                    StageDate =
                        finalDisplayDate
                };
            }

            if (memoDisplayDate == today &&
                !excluirMemoPorBindPlant)
            {
                return new StageDashboardInfo
                {
                    StageKey =
                        "MemoExtract",

                    StageName =
                        "Memo Extract",

                    CompletionStatus =
                        "Memo Extract Completed",

                    StageDate =
                        memoDisplayDate
                };
            }

            if (finalPODisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey =
                        "FinalPO",

                    StageName =
                        ObtenerNombreEtapaFinalPO(
                            bindPlantCodes),

                    CompletionStatus =
                        "Final PO Sent",

                    StageDate =
                        finalPODisplayDate
                };
            }

            if (shippingDisplayDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey =
                        "Shipping",

                    StageName =
                        "Shipping Date",

                    CompletionStatus =
                        "Shipped Pages",

                    StageDate =
                        shippingDisplayDate
                };
            }

            if (item.DirxionDate == today)
            {
                return new StageDashboardInfo
                {
                    StageKey =
                        "Dirxion",

                    StageName =
                        "Dirxion Date",

                    CompletionStatus =
                        "Dirxion Sent",

                    StageDate =
                        item.DirxionDate
                };
            }

            return null;
        }

        private static DateOnly ObtenerFinalPODisplayDate(
            RawAssignmentDashboardDto item,
            bool esIveau,
            bool esAu,
            bool esNz)
        {
            if (esNz)
            {
                return GetSameOrNextBusinessDay(
                    item.MemoExtract);
            }

            if (esAu)
            {
                if (item.MemoExtract == default)
                {
                    return default;
                }

                return GetSameOrNextBusinessDay(
                    item.MemoExtract.AddDays(1));
            }

            if (esIveau)
            {
                return GetPreviousWorkDate(
                    item.FinalPODate,
                    3);
            }

            return item.FinalPODate;
        }

        private static string ObtenerNombreEtapaFinalPO(
            HashSet<string> bindPlantCodes)
        {
            foreach (var code in NamedFinalPoBindPlants)
            {
                if (bindPlantCodes.Contains(code))
                {
                    return $"Final PO {code}";
                }
            }

            return "Final PO Date";
        }

        private static HashSet<string> ObtenerCodigosBindPlant(
            string? bindPlantName)
        {
            var result =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(
                    bindPlantName))
            {
                return result;
            }

            var normalized =
                bindPlantName
                    .Trim()
                    .ToUpperInvariant();

            result.Add(normalized);

            var parts =
                normalized.Split(
                    BindPlantSeparators,
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                result.Add(part);
            }

            return result;
        }

        private static bool EstaEtapaCompletada(
            AssignmentWorkState? state,
            string stageKey)
        {
            if (state is null)
            {
                return false;
            }

            return stageKey switch
            {
                "ProofExtract" =>
                    state.ProofExtractWorked,

                "FinalExtract" =>
                    state.FinalExtractWorked,

                "MemoExtract" =>
                    state.MemoExtractWorked,

                "FinalPO" =>
                    state.FinalPOWorked,

                "Shipping" =>
                    state.ShippingWorked,

                "Dirxion" =>
                    state.DirxionWorked,

                _ =>
                    false
            };
        }

        private static bool EstaListoParaShipping(
            HashSet<string> bindPlantCodes,
            AssignmentWorkState? state)
        {
            if (state?.FinalPOWorked != true)
            {
                return false;
            }

            if (bindPlantCodes.Contains("IVEAU"))
            {
                return true;
            }

            return
                state.HasFinalPoVerification &&
                state.ClosingCorrectionsVerified &&
                state.LateWorkVerified;
        }

        private static DateOnly GetNextBusinessDay(
            DateOnly sourceDate)
        {
            if (sourceDate == default)
            {
                return default;
            }

            var result =
                sourceDate.AddDays(1);

            while (result.DayOfWeek is
                   DayOfWeek.Saturday or
                   DayOfWeek.Sunday)
            {
                result =
                    result.AddDays(1);
            }

            return result;
        }

        private static DateOnly GetPreviousWorkDate(
            DateOnly sourceDate,
            int daysBefore)
        {
            if (sourceDate == default)
            {
                return default;
            }

            var result =
                sourceDate.AddDays(
                    -daysBefore);

            while (result.DayOfWeek is
                   DayOfWeek.Saturday or
                   DayOfWeek.Sunday)
            {
                result =
                    result.AddDays(-1);
            }

            return result;
        }

        private static DateOnly GetSameOrNextBusinessDay(
            DateOnly sourceDate)
        {
            if (sourceDate == default)
            {
                return default;
            }

            var result =
                sourceDate;

            while (result.DayOfWeek is
                   DayOfWeek.Saturday or
                   DayOfWeek.Sunday)
            {
                result =
                    result.AddDays(1);
            }

            return result;
        }

        private static DateOnly ObtenerFechaHoyRepublicaDominicana()
        {
            var local =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    DominicanTimeZone);

            return DateOnly.FromDateTime(
                local);
        }

        private static TimeZoneInfo ResolveDominicanTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "America/Santo_Domingo");
            }
            catch (
                TimeZoneNotFoundException)
            {
            }
            catch (
                InvalidTimeZoneException)
            {
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "SA Western Standard Time");
            }
            catch (
                TimeZoneNotFoundException)
            {
            }
            catch (
                InvalidTimeZoneException)
            {
            }

            return TimeZoneInfo.CreateCustomTimeZone(
                "Dominican Republic Fallback",
                TimeSpan.FromHours(-4),
                "Dominican Republic",
                "Dominican Republic");
        }

        private static async Task ActualizarFinalizadosPorDirxionAsync(
            AppDbContext db,
            DateOnly today,
            UserDataScope scope)
        {
            var librosParaFinalizar =
                db.Libros
                    .ApplyScope(scope)
                    .Where(l =>
                        !l.Finalizado &&
                        l.DirxionDate < today);

            if (!await librosParaFinalizar.AnyAsync())
            {
                return;
            }

            var libroIds =
                librosParaFinalizar
                    .Select(l => l.Id);

            await db.Evaluaciones
                .Where(ev =>
                    !ev.Finalizado &&
                    libroIds.Contains(
                        ev.Asignacion!.IdLibro))
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        ev => ev.Finalizado,
                        true));

            await db.Asignaciones
                .Where(a =>
                    !a.Finalizado &&
                    libroIds.Contains(
                        a.IdLibro))
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        a => a.Finalizado,
                        true));

            await db.Libros
                .ApplyScope(scope)
                .Where(l =>
                    !l.Finalizado &&
                    l.DirxionDate < today)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        l => l.Finalizado,
                        true));
        }

        private static void ValidarScope(
            UserDataScope scope)
        {
            ArgumentNullException.ThrowIfNull(
                scope);

            if (!scope.ViewAllEmployeeTypes &&
                !scope.EmployeeTypeId.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "The current data scope does not contain an employee type.");
            }
        }

        private sealed class AssignedBookDashboardComparer
            : IComparer<AssignedBookDashboardDto>
        {
            public static AssignedBookDashboardComparer Instance { get; } =
                new();

            public int Compare(
                AssignedBookDashboardDto? x,
                AssignedBookDashboardDto? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x is null)
                {
                    return -1;
                }

                if (y is null)
                {
                    return 1;
                }

                var dateComparison =
                    Nullable.Compare(
                        x.StageDate,
                        y.StageDate);

                if (dateComparison != 0)
                {
                    return dateComparison;
                }

                var employeeComparison =
                    string.Compare(
                        x.EmployeeName,
                        y.EmployeeName,
                        StringComparison.OrdinalIgnoreCase);

                if (employeeComparison != 0)
                {
                    return employeeComparison;
                }

                return string.Compare(
                    x.BookName,
                    y.BookName,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        private sealed class WeeklyEvaluationDashboardComparer
            : IComparer<WeeklyEvaluationDashboardDto>
        {
            public static WeeklyEvaluationDashboardComparer Instance { get; } =
                new();

            public int Compare(
                WeeklyEvaluationDashboardDto? x,
                WeeklyEvaluationDashboardDto? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x is null)
                {
                    return -1;
                }

                if (y is null)
                {
                    return 1;
                }

                var dateComparison =
                    x.ShippingDate.CompareTo(
                        y.ShippingDate);

                if (dateComparison != 0)
                {
                    return dateComparison;
                }

                var employeeComparison =
                    string.Compare(
                        x.EmployeeName,
                        y.EmployeeName,
                        StringComparison.OrdinalIgnoreCase);

                if (employeeComparison != 0)
                {
                    return employeeComparison;
                }

                return string.Compare(
                    x.BookName,
                    y.BookName,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        private sealed class RawAssignmentDashboardDto
        {
            public Guid AssignmentId { get; set; }

            public Guid BookId { get; set; }

            public Guid EmployeeId { get; set; }

            public string EmployeeName { get; set; } =
                string.Empty;

            public string KgenCode { get; set; } =
                string.Empty;

            public string LsaCode { get; set; } =
                string.Empty;

            public string BookName { get; set; } =
                string.Empty;

            public string BindPlantName { get; set; } =
                string.Empty;

            public string Database { get; set; } =
                string.Empty;

            public short EmployeeTypeId { get; set; }

            public string EmployeeTypeCode { get; set; } =
                string.Empty;

            public DateOnly ProofExtract { get; set; }

            public DateOnly FinalExtract { get; set; }

            public DateOnly MemoExtract { get; set; }

            public DateOnly DirxionDate { get; set; }

            public DateOnly FinalPODate { get; set; }

            public DateOnly ShippingDate { get; set; }
        }

        private sealed class RawAssignmentWorkDto
        {
            public Guid AssignmentId { get; set; }

            public bool ProofExtractWorked { get; set; }

            public bool FinalExtractWorked { get; set; }

            public bool MemoExtractWorked { get; set; }

            public bool FinalPOWorked { get; set; }

            public bool ShippingWorked { get; set; }

            public bool DirxionWorked { get; set; }

            public bool ClosingCorrectionsVerified { get; set; }

            public bool LateWorkVerified { get; set; }
        }

        private sealed class AssignmentWorkState
        {
            public bool ProofExtractWorked { get; set; }

            public bool FinalExtractWorked { get; set; }

            public bool MemoExtractWorked { get; set; }

            public bool FinalPOWorked { get; set; }

            public bool ShippingWorked { get; set; }

            public bool DirxionWorked { get; set; }

            public bool HasFinalPoVerification { get; set; }

            public bool ClosingCorrectionsVerified { get; set; }

            public bool LateWorkVerified { get; set; }
        }

        private sealed class RawTemporaryAssignmentDto
        {
            public Guid AssignmentId { get; set; }

            public Guid TemporaryEmployeeId { get; set; }

            public string TemporaryEmployeeName { get; set; } =
                string.Empty;

            public bool Proof { get; set; }

            public bool Final { get; set; }

            public bool Memo { get; set; }

            public bool FinalPO { get; set; }

            public bool Shipping { get; set; }

            public bool Dirxion { get; set; }
        }

        private sealed class RawWeeklyEvaluationDashboardDto
        {
            public Guid EvaluationId { get; set; }

            public Guid AssignmentId { get; set; }

            public Guid BookId { get; set; }

            public Guid EmployeeId { get; set; }

            public string EmployeeName { get; set; } =
                string.Empty;

            public string KgenCode { get; set; } =
                string.Empty;

            public string LsaCode { get; set; } =
                string.Empty;

            public string BookName { get; set; } =
                string.Empty;

            public short EmployeeTypeId { get; set; }

            public string EmployeeTypeCode { get; set; } =
                string.Empty;

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

        private sealed class StageDashboardInfo
        {
            public string StageKey { get; set; } =
                string.Empty;

            public string StageName { get; set; } =
                string.Empty;

            public string CompletionStatus { get; set; } =
                string.Empty;

            public DateOnly StageDate { get; set; }
        }
    }
}