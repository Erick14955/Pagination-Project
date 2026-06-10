using System.Security.Claims;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Pagination_Project.Data;
using Pagination_Project.Models;
using System.IO.Compression;

namespace Pagination_Project.Services
{
    public class PaginationChecklistService : IPaginationChecklistService
    {
        private const string TemplateVivial = "Pagination Checklist Vivial.xlsm";
        private const string TemplateWpur = "Pagination Checklist WPUR.xlsm";
        private const string TemplateAuNz = "Pagination Checklist AU-NZ.xlsm";

        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IWebHostEnvironment _env;

        public PaginationChecklistService(
            IDbContextFactory<AppDbContext> dbFactory,
            IWebHostEnvironment env)
        {
            _dbFactory = dbFactory;
            _env = env;
        }

        public async Task<PaginationChecklistDownloadResult> GeneratePaginationChecklistAsync(
            Guid bookId,
            Guid assignmentId,
            ClaimsPrincipal user)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var book = await (
                from l in db.Libros.AsNoTracking()

                join d in db.Databases.AsNoTracking()
                    on l.DatabaseId equals d.Id into databaseGroup
                from d in databaseGroup.DefaultIfEmpty()

                join bp in db.Bind_Plants.AsNoTracking()
                    on l.BindPlantId equals bp.Id into bindPlantGroup
                from bp in bindPlantGroup.DefaultIfEmpty()

                join pf in db.Print_Foots.AsNoTracking()
                    on l.PrintFootId equals pf.Id into printFootGroup
                from pf in printFootGroup.DefaultIfEmpty()

                join lc in db.Legacy_Codes.AsNoTracking()
                    on l.LegacyCodeId equals lc.Id into legacyCodeGroup
                from lc in legacyCodeGroup.DefaultIfEmpty()

                where l.Id == bookId

                select new BookChecklistData
                {
                    BookId = l.Id,
                    BookName = l.BookName ?? string.Empty,
                    KgenCode = l.KGENCode ?? string.Empty,
                    LsaCode = l.LSACode ?? string.Empty,

                    FootPrint = pf.Print_Foot_Name?? string.Empty,
                    LegacyCode = lc.Legacy_Code_Name ?? string.Empty,

                    GraphicsDatabase = d != null ? d.Database_Name ?? string.Empty : string.Empty,
                    BindPlantName = bp != null ? bp.Bind_Plant_Name ?? string.Empty : string.Empty,

                    Nwp = l.NWP,
                    SrlSuppression = l.SRLSuppression
                }
            ).FirstOrDefaultAsync();

            if (book is null)
                throw new InvalidOperationException("The selected book was not found.");

            var pagerName = await ObtenerPagerNameAsync(db, assignmentId, user);

            var templateName = SeleccionarPlantilla(book.BindPlantName);
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "templates",
                "checklists",
                templateName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Checklist template not found: {templateName}");
            }

            var templateBytes = await File.ReadAllBytesAsync(templatePath);

            using var stream = new MemoryStream();
            await stream.WriteAsync(templateBytes);
            stream.Position = 0;

            using (var document = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = document.WorkbookPart
                    ?? throw new InvalidOperationException("Invalid Excel template.");

                var sheetsToFill = ObtenerHojasParaLlenar(templateName);

                foreach (var sheetName in sheetsToFill)
                {
                    var worksheetPart = GetWorksheetPartByName(workbookPart, sheetName);

                    if (worksheetPart is null)
                        continue;

                    LlenarDatosGenerales(worksheetPart, book, pagerName);
                }

                MarcarNwp(workbookPart, templateName, book.Nwp);
                MarcarSrlSuppression(workbookPart, templateName, book.SrlSuppression);

                if (workbookPart.Workbook is null)
                    throw new InvalidOperationException("Invalid Excel template. Workbook not found.");

                workbookPart.Workbook.Save();
            }

            var cleanKgen = SanitizeFilePart(book.KgenCode);

            var xlsmFileName = $"Pagination Checklist {cleanKgen}.xlsm";
            var zipFileName = $"Pagination Checklist {cleanKgen}.zip";

            var xlsmBytes = stream.ToArray();

            var zipBytes = CrearZipConChecklist(
                xlsmBytes,
                xlsmFileName);

            return new PaginationChecklistDownloadResult
            {
                Content = zipBytes,
                FileName = zipFileName,
                ContentType = "application/zip"
            };
        }

        private static byte[] CrearZipConChecklist(
         byte[] checklistBytes,
        string checklistFileName)
        {
            using var zipStream = new MemoryStream();

            using (var archive = new ZipArchive(
                zipStream,
                ZipArchiveMode.Create,
                leaveOpen: true))
            {
                var entry = archive.CreateEntry(
                    checklistFileName,
                    CompressionLevel.Fastest);

                using var entryStream = entry.Open();
                entryStream.Write(checklistBytes, 0, checklistBytes.Length);
            }

            return zipStream.ToArray();
        }

        private static string SeleccionarPlantilla(string bindPlantName)
        {
            if (EsBindPlant(bindPlantName, "WAUK", "LMRA", "SUSX"))
                return TemplateVivial;

            if (EsBindPlant(bindPlantName, "DIRX", "PREM"))
                return TemplateWpur;

            if (EsBindPlant(bindPlantName, "IVEAU", "WSTR", "AU", "NZ"))
                return TemplateAuNz;

            throw new InvalidOperationException(
                $"No checklist template configured for Bind Plant: {bindPlantName}");
        }

        private static List<string> ObtenerHojasParaLlenar(string templateName)
        {
            if (string.Equals(templateName, TemplateVivial, StringComparison.OrdinalIgnoreCase))
            {
                return new List<string>
                {
                    "PROOF PAGES",
                    "FINAL PAGES",
                    "MEMO PAGES"
                };
            }

            return new List<string>
            {
                "PROOF PAGES",
                "FINAL PAGES"
            };
        }

        private static void LlenarDatosGenerales(
            WorksheetPart worksheetPart,
            BookChecklistData book,
            string pagerName)
        {
            SetCellText(worksheetPart, "E2", pagerName);
            SetCellText(worksheetPart, "E3", book.FootPrint);
            SetCellText(worksheetPart, "E4", book.LegacyCode);
            SetCellText(worksheetPart, "E5", book.LsaCode);
            SetCellText(worksheetPart, "E6", book.KgenCode);
            SetCellText(worksheetPart, "E7", book.BookName);
            SetCellText(worksheetPart, "E8", book.GraphicsDatabase);
        }

        private static void MarcarNwp(
            WorkbookPart workbookPart,
            string templateName,
            bool nwp)
        {
            var proof = GetWorksheetPartByName(workbookPart, "PROOF PAGES");
            var final = GetWorksheetPartByName(workbookPart, "FINAL PAGES");
            var memo = GetWorksheetPartByName(workbookPart, "MEMO PAGES");

            if (proof is not null)
            {
                // PROOF - NWP Section: YES M27 / NO O27
                SetYesNo(proof, yesCell: "M27", noCell: "O27", valueIsYes: nwp);
            }

            if (final is not null)
            {
                // FINAL - NWP Section - Verify Process: YES B28 / NO D28
                SetYesNo(final, yesCell: "B28", noCell: "D28", valueIsYes: nwp);

                // FINAL - NWP Section - Reported: YES K28 / NO O28
                SetYesNo(final, yesCell: "K28", noCell: "O28", valueIsYes: nwp);
            }

            if (string.Equals(templateName, TemplateVivial, StringComparison.OrdinalIgnoreCase)
                && memo is not null)
            {
                // MEMO - Variance Report / NWP if any: YES S16 / NO U16
                SetYesNo(memo, yesCell: "S16", noCell: "U16", valueIsYes: nwp);
            }
        }

        private static void MarcarSrlSuppression(
            WorkbookPart workbookPart,
            string templateName,
            bool srlSuppression)
        {
            // WPUR no trae sección SRL Suppression en estas plantillas.
            if (string.Equals(templateName, TemplateWpur, StringComparison.OrdinalIgnoreCase))
                return;

            var proof = GetWorksheetPartByName(workbookPart, "PROOF PAGES");
            var final = GetWorksheetPartByName(workbookPart, "FINAL PAGES");
            var memo = GetWorksheetPartByName(workbookPart, "MEMO PAGES");

            if (proof is not null)
            {
                // PROOF - SRL Suppression: YES K49 / NO M49
                SetYesNo(proof, yesCell: "K49", noCell: "M49", valueIsYes: srlSuppression);
            }

            if (final is not null)
            {
                // FINAL - SRL Suppression: YES K54 / NO M54
                SetYesNo(final, yesCell: "K54", noCell: "M54", valueIsYes: srlSuppression);
            }

            if (string.Equals(templateName, TemplateVivial, StringComparison.OrdinalIgnoreCase)
                && memo is not null)
            {
                // MEMO - SRL Suppression: YES K28 / NO M28
                SetYesNo(memo, yesCell: "K28", noCell: "M28", valueIsYes: srlSuppression);
            }
        }

        private static void SetYesNo(
            WorksheetPart worksheetPart,
            string yesCell,
            string noCell,
            bool valueIsYes)
        {
            SetCellBoolean(worksheetPart, yesCell, valueIsYes);
            SetCellBoolean(worksheetPart, noCell, !valueIsYes);
        }

        private async Task<string> ObtenerPagerNameAsync(
            AppDbContext db,
            Guid assignmentId,
            ClaimsPrincipal user)
        {
            var employeeClaim =
                user.FindFirst("EmployeeName")?.Value ??
                user.FindFirst("EmpleadoNombre")?.Value ??
                user.FindFirst("LinkedEmployeeName")?.Value;

            if (!string.IsNullOrWhiteSpace(employeeClaim))
                return employeeClaim.Trim();

            var temporaryEmployeeName = await db.TemporaryAssignments
                .AsNoTracking()
                .Include(x => x.TemporaryEmployee)
                .Where(x =>
                    x.Active &&
                    x.Proof &&
                    x.AssignmentId == assignmentId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.TemporaryEmployee != null
                    ? x.TemporaryEmployee.Nombre
                    : null)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(temporaryEmployeeName))
                return temporaryEmployeeName.Trim();

            var assignedEmployeeName = await (
                from a in db.Asignaciones.AsNoTracking()
                join e in db.Empleados.AsNoTracking() on a.IdEmpleado equals e.Id
                where a.Id == assignmentId
                select e.Nombre ?? string.Empty
            ).FirstOrDefaultAsync();

            return assignedEmployeeName?.Trim() ?? string.Empty;
        }

        private static WorksheetPart? GetWorksheetPartByName(
            WorkbookPart workbookPart,
            string sheetName)
        {
            if (workbookPart.Workbook is null)
                return null;

            var sheet = workbookPart.Workbook
                .Descendants<Sheet>()
                .FirstOrDefault(s => string.Equals(
                    s.Name?.Value,
                    sheetName,
                    StringComparison.OrdinalIgnoreCase));

            if (sheet?.Id?.Value is null)
                return null;

            return workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart;
        }

        private static void SetCellText(
            WorksheetPart worksheetPart,
            string cellReference,
            string? value)
        {
            var cell = GetOrCreateCell(worksheetPart, cellReference);

            cell.RemoveAllChildren<CellFormula>();
            cell.RemoveAllChildren<CellValue>();
            cell.RemoveAllChildren<InlineString>();

            cell.DataType = CellValues.InlineString;
            cell.InlineString = new InlineString(
                new Text(value ?? string.Empty)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });

            if (worksheetPart.Worksheet is null)
                throw new InvalidOperationException("Invalid Excel template. Worksheet not found.");

            worksheetPart.Worksheet.Save();
        }

        private static void SetCellBoolean(
            WorksheetPart worksheetPart,
            string cellReference,
            bool value)
        {
            var cell = GetOrCreateCell(worksheetPart, cellReference);

            cell.RemoveAllChildren<CellFormula>();
            cell.RemoveAllChildren<InlineString>();

            cell.DataType = CellValues.Boolean;
            cell.CellValue = new CellValue(value ? "1" : "0");

            if (worksheetPart.Worksheet is null)
                throw new InvalidOperationException("Invalid Excel template. Worksheet not found.");

            worksheetPart.Worksheet.Save();
        }

        private static Cell GetOrCreateCell(
            WorksheetPart worksheetPart,
            string cellReference)
        {
            if (worksheetPart.Worksheet is null)
                throw new InvalidOperationException("Invalid Excel template. Worksheet not found.");

            var worksheet = worksheetPart.Worksheet;

            var sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData is null)
            {
                sheetData = new SheetData();
                worksheet.Append(sheetData);
            }

            var rowIndex = GetRowIndex(cellReference);
            var columnName = GetColumnName(cellReference);
            var columnIndex = GetColumnIndex(columnName);

            var row = sheetData
                .Elements<Row>()
                .FirstOrDefault(r => r.RowIndex is not null && r.RowIndex.Value == rowIndex);

            if (row is null)
            {
                row = new Row { RowIndex = rowIndex };

                var refRow = sheetData
                    .Elements<Row>()
                    .FirstOrDefault(r => r.RowIndex is not null && r.RowIndex.Value > rowIndex);

                sheetData.InsertBefore(row, refRow);
            }

            var cell = row
                .Elements<Cell>()
                .FirstOrDefault(c => string.Equals(
                    c.CellReference?.Value,
                    cellReference,
                    StringComparison.OrdinalIgnoreCase));

            if (cell is not null)
                return cell;

            cell = new Cell { CellReference = cellReference };

            var refCell = row
                .Elements<Cell>()
                .FirstOrDefault(c =>
                {
                    var existingCellReference = c.CellReference?.Value;

                    if (string.IsNullOrWhiteSpace(existingCellReference))
                        return false;

                    var existingColumn = GetColumnName(existingCellReference);
                    var existingIndex = GetColumnIndex(existingColumn);

                    return existingIndex > columnIndex;
                });

            row.InsertBefore(cell, refCell);

            return cell;
        }

        private static uint GetRowIndex(string cellReference)
        {
            var rowText = new string(cellReference.Where(char.IsDigit).ToArray());

            return uint.TryParse(rowText, out var rowIndex)
                ? rowIndex
                : 1;
        }

        private static string GetColumnName(string cellReference)
        {
            return new string(cellReference.Where(char.IsLetter).ToArray()).ToUpperInvariant();
        }

        private static int GetColumnIndex(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return 0;

            var index = 0;

            foreach (var ch in columnName.ToUpperInvariant())
            {
                index *= 26;
                index += ch - 'A' + 1;
            }

            return index;
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

        private static string SanitizeFilePart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Book";

            var invalidChars = Path.GetInvalidFileNameChars();
            var clean = new string(value
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            clean = clean.Replace(" ", "_").Trim();

            return string.IsNullOrWhiteSpace(clean)
                ? "Book"
                : clean;
        }

        private sealed class BookChecklistData
        {
            public Guid BookId { get; set; }

            public string BookName { get; set; } = string.Empty;
            public string KgenCode { get; set; } = string.Empty;
            public string LsaCode { get; set; } = string.Empty;
            public string FootPrint { get; set; } = string.Empty;
            public string LegacyCode { get; set; } = string.Empty;
            public string GraphicsDatabase { get; set; } = string.Empty;
            public string BindPlantName { get; set; } = string.Empty;

            public bool Nwp { get; set; }
            public bool SrlSuppression { get; set; }
        }
    }
}