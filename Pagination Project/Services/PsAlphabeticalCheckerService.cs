using Microsoft.AspNetCore.Components.Forms;
using Pagination_Project.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pagination_Project.Services;

public sealed class PsAlphabeticalCheckerService : IPsAlphabeticalCheckerService
{
    private const long MaxFileSize = 250L * 1024L * 1024L;
    private const int MaxFiles = 250;
    private const string AlgorithmVersion = "transition-aware-2026.09.08.7";

    private static readonly Regex PageFolioRegex = new(
        @"FOLIO-(\d{4,})",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex FooterPageRegex = new(
        @"PAGE:(\d{4,})",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex PagesCountRegex = new(
        @"%%Pages:\s*(\d+)",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex FontRegex = new(
        @"/(BellCentennial-NameAndNumber|NewsGothic-Bold)\s+[^/\r\n]{0,160}?\bty\b",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex SectionHeaderFontRegex = new(
        @"/FranklinGothic-CondensedYP\s+[^/\r\n]{0,160}?\bty\b",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex ColumnWidthRegex = new(
        @"(?<width>-?\d+(?:\.\d+)?)\s+cw\b",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex AnyFontRegex = new(
        @"/[A-Za-z0-9_.-]+\s+[^/\r\n]{0,160}?\bty\b",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex PsPrintedStringRegex = new(
        @"(?<text>\((?:\\.|[^()\\])*\))\s+1\.00\s+prt",
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex MultipleWhitespaceRegex = new(
        @"\s+",
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex LeadingNumberRegex = new(
        @"^\s*(?<number>\d[\d,]*)",
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex TrailingSeparatorRegex = new(
        @"\s*[-–—]\s*$",
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex PhoneOnlyRegex = new(
        @"^(?:(?:phone|ph|telephone|tel|fax|facsimile|mobile|mob|freecall|free\s+call|local\s+call|enquiries?|contact)\.?\s*[:\-]?\s*)?[\d\s()+./\-]+(?:[A-Z]{2,8})?$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex AuxiliaryLabelRegex = new(
        @"^\s*(?:fax|facsimile|phone|ph|telephone|tel|mobile|mob|freecall|free\s+call|local\s+call|enquiries?|contact|email|e-mail|website|web|address|office|reception|appointments?|after\s+hours|hours|opening\s+hours)\b",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex AddressPrefixRegex = new(
        @"^\s*(?:unit|un|suite|shop|sh|room|rm|level|lvl|floor|fl|building|bldg|po\s+box|p\.?\s*o\.?\s*box)\b",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex NumericAddressRegex = new(
        @"^\s*\d{1,6}\s+[A-Za-z]",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex AddressRoadRegex = new(
        @"\b(?:rd|road|st|street|ave|avenue|dr|drive|pl|place|cres|crescent|ct|court|ln|lane|hwy|highway|pde|parade|tce|terrace|cct|circuit|way|close|cl)\b",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex HonorificRegex = new(
        @"^\s*(?:DR|MR|MRS|MS|MISS|PROF|PROFESSOR)\.?\s+",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex MountAbbreviationRegex = new(
        @"^\s*MT\.?\s+",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    private static readonly Regex SaintAbbreviationRegex = new(
        @"^\s*ST\.?\s+",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    public async Task<PsBookAnalysisResult> AnalyzeBookAsync(
        IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default)
    {
        ValidateFiles(files);

        var parsedPages = new List<ParsedPsPage>();

        var warnings = new List<string>
        {
            $"Checker engine: {AlgorithmVersion}"
        };

        for (var fileIndex = 0;
             fileIndex < files.Count;
             fileIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = files[fileIndex];

            var ps =
                await ReadPostScriptAsync(
                    file,
                    cancellationToken);

            var declaredPages =
                DetectDeclaredPageCount(ps);

            if (declaredPages is > 1)
            {
                warnings.Add(
                    $"{file.Name} declares {declaredPages} PostScript pages. This checker is optimized for the supplied format where each .ps file represents one book page.");
            }

            var pageLabel =
                DetectPageLabel(
                    ps,
                    fileIndex + 1);

            var names =
                ExtractListingNames(ps);

            var sectionLetter =
                DetectSectionLetter(
                    ps,
                    names);

            if (names.Count == 0)
            {
                warnings.Add(
                    $"No listings were detected on page {pageLabel} ({file.Name}).");
            }

            parsedPages.Add(
                new ParsedPsPage(
                    FileName: file.Name,
                    OriginalFileOrder: fileIndex + 1,
                    FileOrder: fileIndex + 1,
                    PageLabel: pageLabel,
                    SectionLetter: sectionLetter,
                    ListingNames: names));
        }

        var pages =
            OrderPages(parsedPages);

        var listings =
            FlattenListings(pages);

        if (listings.Count == 0)
        {
            return new PsBookAnalysisResult
            {
                FilesAnalyzed = files.Count,
                PagesAnalyzed = pages.Count,
                ListingsAnalyzed = 0,
                Errors = [],
                Warnings =
                [
                    .. warnings,
                    "No valid listings could be identified in the uploaded book."
                ]
            };
        }

        var errors =
            FindOutOfOrderListings(
                listings,
                pages);

        return new PsBookAnalysisResult
        {
            FilesAnalyzed = files.Count,
            PagesAnalyzed = pages.Count,
            ListingsAnalyzed = listings.Count,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static List<ParsedPsPage> OrderPages(
        IReadOnlyList<ParsedPsPage> pages)
    {
        return pages
            .OrderBy(
                page =>
                    GetPageNumberForSorting(
                        page.PageLabel))
            .ThenBy(
                page =>
                    page.OriginalFileOrder)
            .Select(
                (page, index) =>
                    page with
                    {
                        FileOrder = index + 1
                    })
            .ToList();
    }

    private static void ValidateFiles(
        IReadOnlyList<IBrowserFile> files)
    {
        if (files.Count == 0)
        {
            throw new InvalidOperationException(
                "Select at least one .ps file.");
        }

        if (files.Count > MaxFiles)
        {
            throw new InvalidOperationException(
                $"A maximum of {MaxFiles} .ps files can be analyzed at once.");
        }

        foreach (var file in files)
        {
            if (!file.Name.EndsWith(
                    ".ps",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"'{file.Name}' is not a .ps file.");
            }

            if (file.Size > MaxFileSize)
            {
                throw new InvalidOperationException(
                    $"'{file.Name}' exceeds the 250 MB limit.");
            }
        }
    }

    private static async Task<string> ReadPostScriptAsync(
        IBrowserFile file,
        CancellationToken cancellationToken)
    {
        await using var stream =
            file.OpenReadStream(
                MaxFileSize,
                cancellationToken);

        using var reader =
            new StreamReader(
                stream,
                Encoding.Latin1,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 64 * 1024,
                leaveOpen: false);

        return await reader.ReadToEndAsync(
            cancellationToken);
    }

    private static int? DetectDeclaredPageCount(
        string ps)
    {
        var match =
            PagesCountRegex.Match(ps);

        return
            match.Success &&
            int.TryParse(
                match.Groups[1].Value,
                out var count)
                ? count
                : null;
    }

    private static string DetectPageLabel(
        string ps,
        int fallbackPage)
    {
        var folioMatches =
            PageFolioRegex.Matches(ps);

        if (folioMatches.Count > 0)
        {
            return NormalizePageNumber(
                folioMatches[^1]
                    .Groups[1]
                    .Value);
        }

        var footerMatches =
            FooterPageRegex.Matches(ps);

        if (footerMatches.Count > 0)
        {
            return NormalizePageNumber(
                footerMatches[^1]
                    .Groups[1]
                    .Value);
        }

        return fallbackPage.ToString(
            CultureInfo.InvariantCulture);
    }

    private static string NormalizePageNumber(
        string raw)
    {
        return int.TryParse(
            raw,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var page)
                ? page.ToString(
                    CultureInfo.InvariantCulture)
                : raw.TrimStart('0');
    }

    private static char DetectSectionLetter(
        string ps,
        IReadOnlyList<string> listingNames)
    {
        foreach (Match fontMatch
                 in SectionHeaderFontRegex.Matches(ps))
        {
            var start =
                fontMatch.Index +
                fontMatch.Length;

            var limit =
                Math.Min(
                    ps.Length,
                    start + 1200);

            var nextFont =
                AnyFontRegex.Match(
                    ps,
                    start);

            if (nextFont.Success &&
                nextFont.Index < limit)
            {
                limit =
                    nextFont.Index;
            }

            if (limit <= start)
            {
                continue;
            }

            var source =
                ps.Substring(
                    start,
                    limit - start);

            foreach (Match textMatch
                     in PsPrintedStringRegex.Matches(source))
            {
                var text =
                    DecodePostScriptString(
                            textMatch
                                .Groups["text"]
                                .Value)
                        .Trim();

                if (text.Length != 1)
                {
                    continue;
                }

                var letter =
                    char.ToUpperInvariant(
                        text[0]);

                if (IsValidLetter(letter))
                {
                    return letter;
                }
            }
        }

        var letters =
            listingNames
                .Where(
                    name =>
                        !IsHonorificListing(name))
                .Select(
                    GetAlphabeticalLetter)
                .Where(
                    IsValidLetter)
                .ToList();

        if (letters.Count < 3)
        {
            return '\0';
        }

        var dominant =
            letters
                .GroupBy(x => x)
                .Select(
                    group =>
                        new
                        {
                            Letter = group.Key,
                            Count = group.Count()
                        })
                .OrderByDescending(
                    x => x.Count)
                .First();

        var confidence =
            (double)dominant.Count /
            letters.Count;

        return confidence >= 0.70
            ? dominant.Letter
            : '\0';
    }

    private static List<string> ExtractListingNames(
        string ps)
    {
        var result =
            new List<string>();

        var fontMatches =
            FontRegex.Matches(ps);

        foreach (Match fontMatch
                 in fontMatches)
        {
            if (!IsAtMainListingColumn(
                    ps,
                    fontMatch.Index,
                    fontMatch.Length))
            {
                continue;
            }

            if (IsPhoneOrNumberBlock(
                    ps,
                    fontMatch.Index))
            {
                continue;
            }

            var start =
                fontMatch.Index +
                fontMatch.Length;

            var end =
                FindCandidateEnd(
                    ps,
                    start);

            if (end <= start)
            {
                continue;
            }

            var candidateSource =
                ps.Substring(
                    start,
                    end - start);

            var text =
                ExtractPrintedText(
                    candidateSource);

            text =
                CleanListingName(
                    text);

            if (!IsLikelyListingName(
                    text))
            {
                continue;
            }

            if (IsMastheadOrFooterText(
                    text))
            {
                continue;
            }

            result.Add(text);
        }

        return result;
    }

    private static bool IsAtMainListingColumn(
        string ps,
        int fontIndex,
        int fontLength)
    {
        var contextStart =
            Math.Max(
                0,
                fontIndex - 650);

        var context =
            ps.Substring(
                contextStart,
                fontIndex -
                contextStart);

        var widths =
            ColumnWidthRegex.Matches(
                context);

        if (widths.Count > 0)
        {
            var lastWidth =
                widths[^1]
                    .Groups["width"]
                    .Value;

            if (double.TryParse(
                    lastWidth,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var width) &&
                Math.Abs(
                    width - 126.0) < 0.01)
            {
                return true;
            }
        }

        var searchStart =
            fontIndex +
            fontLength;

        if (searchStart >= ps.Length)
        {
            return false;
        }

        var searchLimit =
            Math.Min(
                ps.Length,
                searchStart + 1200);

        var nextFont =
            AnyFontRegex.Match(
                ps,
                searchStart);

        if (nextFont.Success &&
            nextFont.Index < searchLimit)
        {
            searchLimit =
                nextFont.Index;
        }

        if (searchLimit <= searchStart)
        {
            return false;
        }

        var forwardContext =
            ps.Substring(
                searchStart,
                searchLimit -
                searchStart);

        var forwardWidths =
            ColumnWidthRegex.Matches(
                forwardContext);

        foreach (Match widthMatch
                 in forwardWidths)
        {
            if (!double.TryParse(
                    widthMatch
                        .Groups["width"]
                        .Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var width))
            {
                continue;
            }

            if (Math.Abs(
                    width - 126.0) < 0.01)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPhoneOrNumberBlock(
        string ps,
        int fontIndex)
    {
        var contextStart =
            Math.Max(
                0,
                fontIndex - 320);

        var context =
            ps.Substring(
                contextStart,
                fontIndex -
                contextStart);

        var lastColumnWidth =
            context.LastIndexOf(
                " cw",
                StringComparison.OrdinalIgnoreCase);

        var lastSld =
            context.LastIndexOf(
                "sld",
                StringComparison.OrdinalIgnoreCase);

        var lastSsw =
            context.LastIndexOf(
                "ssw",
                StringComparison.OrdinalIgnoreCase);

        return
            lastSld >= 0 &&
            lastSsw >= 0 &&
            lastSld > lastColumnWidth &&
            lastSsw > lastColumnWidth;
    }

    private static int FindCandidateEnd(
        string ps,
        int start)
    {
        var limit =
            Math.Min(
                ps.Length,
                start + 1800);

        var candidates =
            new List<int>();

        AddIndex(
            candidates,
            ps.IndexOf(
                "121.00 cw",
                start,
                StringComparison.OrdinalIgnoreCase),
            start,
            limit);

        AddIndex(
            candidates,
            ps.IndexOf(
                "126.00 cw",
                start,
                StringComparison.OrdinalIgnoreCase),
            start,
            limit);

        var nextFont =
            AnyFontRegex.Match(
                ps,
                start);

        if (nextFont.Success)
        {
            AddIndex(
                candidates,
                nextFont.Index,
                start,
                limit);
        }

        return candidates.Count == 0
            ? limit
            : candidates.Min();
    }

    private static void AddIndex(
        List<int> values,
        int index,
        int start,
        int limit)
    {
        if (index > start &&
            index <= limit)
        {
            values.Add(index);
        }
    }

    private static string ExtractPrintedText(
        string source)
    {
        var builder =
            new StringBuilder();

        foreach (Match match
                 in PsPrintedStringRegex.Matches(source))
        {
            var decoded =
                DecodePostScriptString(
                    match
                        .Groups["text"]
                        .Value);

            builder.Append(decoded);
        }

        return builder.ToString();
    }

    private static string DecodePostScriptString(
        string literal)
    {
        if (literal.Length < 2)
        {
            return literal;
        }

        var source =
            literal.AsSpan(
                1,
                literal.Length - 2);

        var builder =
            new StringBuilder(
                source.Length);

        for (var i = 0;
             i < source.Length;
             i++)
        {
            var c =
                source[i];

            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            if (i + 1 >= source.Length)
            {
                break;
            }

            var next =
                source[++i];

            switch (next)
            {
                case 'n':
                    builder.Append('\n');
                    break;

                case 'r':
                    builder.Append('\r');
                    break;

                case 't':
                    builder.Append('\t');
                    break;

                case 'b':
                    builder.Append('\b');
                    break;

                case 'f':
                    builder.Append('\f');
                    break;

                case '(':
                case ')':
                case '\\':
                    builder.Append(next);
                    break;

                case '\r':
                    if (i + 1 <
                        source.Length &&
                        source[i + 1] == '\n')
                    {
                        i++;
                    }

                    break;

                case '\n':
                    break;

                default:
                    if (next is >= '0' and <= '7')
                    {
                        var octal =
                            new StringBuilder(3);

                        octal.Append(next);

                        for (var j = 0;
                             j < 2 &&
                             i + 1 <
                             source.Length;
                             j++)
                        {
                            var digit =
                                source[i + 1];

                            if (digit is < '0' or > '7')
                            {
                                break;
                            }

                            i++;
                            octal.Append(digit);
                        }

                        var code =
                            Convert.ToInt32(
                                octal.ToString(),
                                8);

                        builder.Append(
                            (char)code);
                    }
                    else
                    {
                        builder.Append(next);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private static string CleanListingName(
        string value)
    {
        var cleaned =
            value.Replace(
                '\0',
                ' ');

        cleaned =
            MultipleWhitespaceRegex
                .Replace(
                    cleaned,
                    " ")
                .Trim();

        cleaned =
            TrailingSeparatorRegex
                .Replace(
                    cleaned,
                    string.Empty)
                .Trim();

        return cleaned;
    }

    private static bool IsLikelyListingName(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            text.Length < 2 ||
            text.Length > 220)
        {
            return false;
        }

        var normalized =
            MultipleWhitespaceRegex
                .Replace(
                    text.Trim(),
                    " ");

        if (!normalized.Any(
                char.IsLetter))
        {
            return false;
        }

        if (PhoneOnlyRegex.IsMatch(
                normalized))
        {
            return false;
        }

        if (AuxiliaryLabelRegex.IsMatch(
                normalized))
        {
            return false;
        }

        if (LooksLikeAddressOrContactLine(
                normalized))
        {
            return false;
        }

        return true;
    }

    private static bool LooksLikeAddressOrContactLine(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var normalized =
            MultipleWhitespaceRegex
                .Replace(
                    text.Trim(),
                    " ");

        if (PhoneOnlyRegex.IsMatch(
                normalized))
        {
            return true;
        }

        if (AuxiliaryLabelRegex.IsMatch(
                normalized))
        {
            return true;
        }

        var digitCount =
            normalized.Count(
                char.IsDigit);

        var letterCount =
            normalized.Count(
                char.IsLetter);

        if (digitCount >= 6 &&
            digitCount >= letterCount)
        {
            return true;
        }

        if (AddressPrefixRegex.IsMatch(
                normalized) &&
            digitCount > 0)
        {
            return true;
        }

        if (NumericAddressRegex.IsMatch(
                normalized) &&
            AddressRoadRegex.IsMatch(
                normalized))
        {
            return true;
        }

        if (digitCount > 0 &&
            AddressRoadRegex.IsMatch(
                normalized))
        {
            var firstToken =
                normalized.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(
                    firstToken) &&
                (
                    char.IsDigit(
                        firstToken[0]) ||
                    AddressPrefixRegex.IsMatch(
                        normalized)
                ))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMastheadOrFooterText(
        string text)
    {
        var normalized =
            text.Trim()
                .ToUpperInvariant();

        return
            normalized.Contains(
                "WHITE PAGES DIRECTORY",
                StringComparison.Ordinal)
            ||
            normalized.Contains(
                "THRYV AUSTRALIA PTY LTD",
                StringComparison.Ordinal)
            ||
            normalized.StartsWith(
                "RHD - ",
                StringComparison.Ordinal)
            ||
            normalized.StartsWith(
                "CONFIDENTIAL AND PROPRIETARY",
                StringComparison.Ordinal)
            ||
            normalized.StartsWith(
                "PAGE:",
                StringComparison.Ordinal)
            ||
            normalized == "2026";
    }

    private static List<PsListing> FlattenListings(
        IReadOnlyList<ParsedPsPage> pages)
    {
        var result =
            new List<PsListing>();

        var globalPosition = 0;

        foreach (var page in pages)
        {
            for (var index = 0;
                 index < page.ListingNames.Count;
                 index++)
            {
                globalPosition++;

                var name =
                    page.ListingNames[index];

                result.Add(
                    new PsListing
                    {
                        Name = name,

                        SortKey =
                            BuildSortKey(name),

                        FileName =
                            page.FileName,

                        FileOrder =
                            page.FileOrder,

                        PageOrderInFile = 1,

                        GlobalPageOrder =
                            page.FileOrder,

                        PageLabel =
                            page.PageLabel,

                        PositionInPage =
                            index + 1,

                        GlobalPosition =
                            globalPosition
                    });
            }
        }

        return result;
    }

    private static string BuildSortKey(
        string value)
    {
        var valueForSorting =
            ReplaceLeadingNumberWithWords(
                value);

        valueForSorting =
            NormalizeDirectoryConventions(
                valueForSorting);

        var normalized =
            valueForSorting.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder(
                normalized.Length);

        foreach (var character
                 in normalized)
        {
            var category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character);

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    char.ToUpperInvariant(
                        character));
            }
            else if (
                char.IsWhiteSpace(
                    character) ||
                character is '-' or '/' or
                    '&' or '\'' or '.' or ',')
            {
                builder.Append(' ');
            }
        }

        return MultipleWhitespaceRegex
            .Replace(
                builder.ToString(),
                " ")
            .Trim();
    }

    private static string NormalizeDirectoryConventions(
        string value)
    {
        var normalized = value;

        if (MountAbbreviationRegex.IsMatch(
                normalized))
        {
            normalized =
                MountAbbreviationRegex.Replace(
                    normalized,
                    "Mount ",
                    1);
        }

        if (SaintAbbreviationRegex.IsMatch(
                normalized))
        {
            normalized =
                SaintAbbreviationRegex.Replace(
                    normalized,
                    "Saint ",
                    1);
        }

        return normalized;
    }

    private static bool IsHonorificListing(
        string listingName)
    {
        if (string.IsNullOrWhiteSpace(
                listingName))
        {
            return false;
        }

        return HonorificRegex.IsMatch(
            listingName);
    }

    private static string ReplaceLeadingNumberWithWords(
        string value)
    {
        var match =
            LeadingNumberRegex.Match(
                value);

        if (!match.Success)
        {
            return value;
        }

        var rawNumber =
            match
                .Groups["number"]
                .Value
                .Replace(
                    ",",
                    string.Empty,
                    StringComparison.Ordinal);

        if (!ulong.TryParse(
                rawNumber,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var number))
        {
            var digitWords =
                string.Join(
                    ' ',
                    rawNumber.Select(
                        DigitToWord));

            return
                digitWords +
                value[match.Length..];
        }

        var words =
            NumberToEnglishWords(
                number);

        return
            words +
            value[match.Length..];
    }

    private static string DigitToWord(
        char digit)
    {
        return digit switch
        {
            '0' => "zero",
            '1' => "one",
            '2' => "two",
            '3' => "three",
            '4' => "four",
            '5' => "five",
            '6' => "six",
            '7' => "seven",
            '8' => "eight",
            '9' => "nine",
            _ => string.Empty
        };
    }

    private static string NumberToEnglishWords(
        ulong number)
    {
        if (number == 0)
        {
            return "zero";
        }

        var scales =
            new (ulong Value, string Name)[]
            {
                (
                    1_000_000_000_000_000_000UL,
                    "quintillion"
                ),
                (
                    1_000_000_000_000_000UL,
                    "quadrillion"
                ),
                (
                    1_000_000_000_000UL,
                    "trillion"
                ),
                (
                    1_000_000_000UL,
                    "billion"
                ),
                (
                    1_000_000UL,
                    "million"
                ),
                (
                    1_000UL,
                    "thousand"
                )
            };

        var parts =
            new List<string>();

        var remaining =
            number;

        foreach (var (value, name)
                 in scales)
        {
            if (remaining < value)
            {
                continue;
            }

            var group =
                remaining / value;

            remaining %= value;

            parts.Add(
                $"{NumberBelowOneThousandToWords((int)group)} {name}");
        }

        if (remaining > 0)
        {
            parts.Add(
                NumberBelowOneThousandToWords(
                    (int)remaining));
        }

        return string.Join(
            ' ',
            parts);
    }

    private static string NumberBelowOneThousandToWords(
        int number)
    {
        string[] ones =
        [
            "",
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "ten",
            "eleven",
            "twelve",
            "thirteen",
            "fourteen",
            "fifteen",
            "sixteen",
            "seventeen",
            "eighteen",
            "nineteen"
        ];

        string[] tens =
        [
            "",
            "",
            "twenty",
            "thirty",
            "forty",
            "fifty",
            "sixty",
            "seventy",
            "eighty",
            "ninety"
        ];

        var parts =
            new List<string>();

        if (number >= 100)
        {
            parts.Add(
                $"{ones[number / 100]} hundred");

            number %= 100;
        }

        if (number >= 20)
        {
            parts.Add(
                tens[number / 10]);

            number %= 10;

            if (number > 0)
            {
                parts.Add(
                    ones[number]);
            }
        }
        else if (number > 0)
        {
            parts.Add(
                ones[number]);
        }

        return string.Join(
            ' ',
            parts);
    }

    private static List<PsListingOrderError> FindOutOfOrderListings(
        IReadOnlyList<PsListing> listings,
        IReadOnlyList<ParsedPsPage> pages)
    {
        if (listings.Count < 2)
        {
            return [];
        }

        var errors =
            new List<PsListingOrderError>();

        var errorIds =
            new HashSet<Guid>();

        DetectListingsOutsideAllowedPageRange(
            listings,
            pages,
            errors,
            errorIds);

        DetectPrefixBacktracking(
            listings,
            errors,
            errorIds);

        return errors
            .OrderBy(
                x =>
                    GetPageNumberForSorting(
                        x.CurrentPage))
            .ThenBy(
                x =>
                    x.CurrentPositionInPage)
            .ToList();
    }

    private static void DetectListingsOutsideAllowedPageRange(
        IReadOnlyList<PsListing> listings,
        IReadOnlyList<ParsedPsPage> pages,
        ICollection<PsListingOrderError> errors,
        ISet<Guid> errorIds)
    {
        var pagesByFile =
            pages.ToDictionary(
                parsedPage =>
                    parsedPage.FileOrder);

        for (var i = 0;
             i < listings.Count;
             i++)
        {
            var listing =
                listings[i];

            if (IsHonorificListing(
                    listing.Name))
            {
                continue;
            }

            if (!pagesByFile.TryGetValue(
                    listing.FileOrder,
                    out var parsedPage))
            {
                continue;
            }

            var pageIndex =
                parsedPage.FileOrder - 1;

            var allowedRange =
                GetAllowedSectionRange(
                    pages,
                    pageIndex);

            if (allowedRange is null)
            {
                continue;
            }

            var listingLetter =
                GetAlphabeticalLetter(
                    listing.Name);

            if (!IsValidLetter(
                    listingLetter))
            {
                continue;
            }

            if (listingLetter >=
                    allowedRange.Value.Min &&
                listingLetter <=
                    allowedRange.Value.Max)
            {
                continue;
            }

            AddStructuralError(
                listings,
                i,
                errors,
                errorIds);
        }
    }

    private static SectionRange? GetAllowedSectionRange(
        IReadOnlyList<ParsedPsPage> pages,
        int pageIndex)
    {
        if (pageIndex < 0 ||
            pageIndex >= pages.Count)
        {
            return null;
        }

        var current =
            pages[pageIndex]
                .SectionLetter;

        if (!IsValidLetter(
                current))
        {
            return null;
        }

        var minimum = current;
        var maximum = current;

        if (pageIndex > 0)
        {
            var previous =
                pages[pageIndex - 1]
                    .SectionLetter;

            if (IsValidLetter(
                    previous) &&
                previous < current)
            {
                minimum = previous;
            }
        }

        if (pageIndex <
            pages.Count - 1)
        {
            var next =
                pages[pageIndex + 1]
                    .SectionLetter;

            if (IsValidLetter(
                    next) &&
                next > current)
            {
                maximum = next;
            }
        }
        else
        {
            maximum = 'Z';
        }

        if (minimum > maximum)
        {
            minimum = current;
            maximum = current;
        }

        return new SectionRange(
            minimum,
            maximum);
    }

    private static void DetectPrefixBacktracking(
        IReadOnlyList<PsListing> listings,
        ICollection<PsListingOrderError> errors,
        ISet<Guid> errorIds)
    {
        char activeLetter = '\0';

        var acceptedIndexes =
            new List<int>();

        for (var currentIndex = 0;
             currentIndex < listings.Count;
             currentIndex++)
        {
            var current =
                listings[currentIndex];

            if (errorIds.Contains(
                    current.Id))
            {
                activeLetter = '\0';
                acceptedIndexes.Clear();

                continue;
            }

            if (IsHonorificListing(
                    current.Name))
            {
                continue;
            }

            var currentLetter =
                GetAlphabeticalLetter(
                    current.Name);

            if (!IsValidLetter(
                    currentLetter))
            {
                activeLetter = '\0';
                acceptedIndexes.Clear();

                continue;
            }

            var currentPrefix =
                GetComparablePrefix(
                    current.Name);

            if (currentPrefix is null)
            {
                continue;
            }

            if (activeLetter !=
                currentLetter)
            {
                activeLetter =
                    currentLetter;

                acceptedIndexes.Clear();

                acceptedIndexes.Add(
                    currentIndex);

                continue;
            }

            var currentAccepted = true;

            while (acceptedIndexes.Count > 0)
            {
                var previousIndex =
                    acceptedIndexes[^1];

                var previous =
                    listings[previousIndex];

                var previousPrefix =
                    GetComparablePrefix(
                        previous.Name);

                if (previousPrefix is null)
                {
                    acceptedIndexes.RemoveAt(
                        acceptedIndexes.Count - 1);

                    continue;
                }

                var comparison =
                    string.Compare(
                        currentPrefix,
                        previousPrefix,
                        StringComparison.Ordinal);

                if (comparison >= 0)
                {
                    break;
                }

                var previousIsSpike = false;

                if (acceptedIndexes.Count >= 2)
                {
                    var beforePreviousIndex =
                        acceptedIndexes[
                            acceptedIndexes.Count - 2];

                    var beforePrevious =
                        listings[
                            beforePreviousIndex];

                    var beforePreviousPrefix =
                        GetComparablePrefix(
                            beforePrevious.Name);

                    if (beforePreviousPrefix is not null)
                    {
                        var beforeVsCurrent =
                            string.Compare(
                                beforePreviousPrefix,
                                currentPrefix,
                                StringComparison.Ordinal);

                        if (beforeVsCurrent <= 0)
                        {
                            previousIsSpike = true;
                        }
                    }
                }

                if (previousIsSpike)
                {
                    AddStructuralError(
                        listings,
                        previousIndex,
                        errors,
                        errorIds);

                    acceptedIndexes.RemoveAt(
                        acceptedIndexes.Count - 1);

                    continue;
                }

                AddStructuralError(
                    listings,
                    currentIndex,
                    errors,
                    errorIds);

                currentAccepted = false;

                break;
            }

            if (currentAccepted)
            {
                acceptedIndexes.Add(
                    currentIndex);
            }
        }
    }

    private static char GetAlphabeticalLetter(
        string listingName)
    {
        if (string.IsNullOrWhiteSpace(
                listingName))
        {
            return '\0';
        }

        var sortKey =
            BuildSortKey(
                listingName);

        foreach (var character
                 in sortKey)
        {
            if (IsValidLetter(
                    character))
            {
                return character;
            }
        }

        return '\0';
    }

    private static string? GetComparablePrefix(
        string listingName)
    {
        var sortKey =
            BuildSortKey(
                listingName);

        if (string.IsNullOrWhiteSpace(
                sortKey))
        {
            return null;
        }

        var words =
            sortKey.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            return null;
        }

        var firstWordLetters =
            new string(
                words[0]
                    .Where(
                        char.IsLetter)
                    .ToArray());

        if (firstWordLetters.Length < 2)
        {
            return null;
        }

        return firstWordLetters[..2];
    }

    private static bool IsValidLetter(
        char letter)
    {
        return letter is >= 'A' and <= 'Z';
    }

    private static void AddStructuralError(
        IReadOnlyList<PsListing> listings,
        int currentIndex,
        ICollection<PsListingOrderError> errors,
        ISet<Guid> errorIds)
    {
        var item =
            listings[currentIndex];

        if (!errorIds.Add(
                item.Id))
        {
            return;
        }

        var references =
            listings
                .Where(
                    (listing, index) =>
                        index != currentIndex &&
                        !errorIds.Contains(
                            listing.Id) &&
                        !IsHonorificListing(
                            listing.Name))
                .OrderBy(
                    listing =>
                        listing.SortKey,
                    StringComparer.Ordinal)
                .ThenBy(
                    listing =>
                        listing.GlobalPosition)
                .ToList();

        var itemKey =
            item.SortKey;

        PsListing? previous = null;
        PsListing? next = null;

        foreach (var candidate
                 in references)
        {
            var comparison =
                string.Compare(
                    candidate.SortKey,
                    itemKey,
                    StringComparison.Ordinal);

            if (comparison <= 0)
            {
                previous =
                    candidate;

                continue;
            }

            next =
                candidate;

            break;
        }

        var recommendedFile =
            item.FileName;

        var recommendedPage =
            item.PageLabel;

        var recommendedPosition =
            item.PositionInPage;

        if (next is not null)
        {
            recommendedFile =
                next.FileName;

            recommendedPage =
                next.PageLabel;

            recommendedPosition =
                next.PositionInPage;
        }
        else if (previous is not null)
        {
            recommendedFile =
                previous.FileName;

            recommendedPage =
                previous.PageLabel;

            recommendedPosition =
                previous.PositionInPage + 1;
        }

        errors.Add(
            new PsListingOrderError
            {
                ListingName =
                    item.Name,

                CurrentFile =
                    item.FileName,

                CurrentPage =
                    item.PageLabel,

                CurrentPositionInPage =
                    item.PositionInPage,

                RecommendedFile =
                    recommendedFile,

                RecommendedPage =
                    recommendedPage,

                RecommendedPositionInPage =
                    recommendedPosition,

                ShouldGoAfter =
                    previous?.Name,

                ShouldGoBefore =
                    next?.Name
            });
    }

    private static int GetPageNumberForSorting(
        string pageNumber)
    {
        return int.TryParse(
            pageNumber,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
                ? value
                : int.MaxValue;
    }

    private sealed record ParsedPsPage(
        string FileName,
        int OriginalFileOrder,
        int FileOrder,
        string PageLabel,
        char SectionLetter,
        IReadOnlyList<string> ListingNames);

    private readonly record struct SectionRange(
        char Min,
        char Max);
}