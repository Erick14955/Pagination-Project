using Microsoft.AspNetCore.Components.Forms;
using Pagination_Project.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pagination_Project.Services;

public sealed class PsAlphabeticalCheckerService : IPsAlphabeticalCheckerService
{
    private const long MaxFileSize = 250L * 1024L * 1024L;
    private const int MaxFiles = 100;

    private static readonly Regex PageFolioRegex = new(
        @"FOLIO-(\d{4,})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex FooterPageRegex = new(
        @"PAGE:(\d{4,})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PagesCountRegex = new(
        @"%%Pages:\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex FontRegex = new(
        @"/(BellCentennial-NameAndNumber|NewsGothic-Bold)\s+[^/\r\n]{0,160}?\bty\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ColumnWidthRegex = new(
        @"(?<width>-?\d+(?:\.\d+)?)\s+cw\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AnyFontRegex = new(
        @"/[A-Za-z0-9_.-]+\s+[^/\r\n]{0,160}?\bty\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PsPrintedStringRegex = new(
        @"(?<text>\((?:\\.|[^()\\])*\))\s+1\.00\s+prt",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex MultipleWhitespaceRegex = new(
        @"\s+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LeadingNumberRegex = new(
        @"^\s*(?<number>\d[\d,]*)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TrailingSeparatorRegex = new(
        @"(?:\s+-\s*|\s+–\s*|\s+—\s*)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PhoneOnlyRegex = new(
        @"^(?:phone|fax|mobile|freecall|local\s+call|enquiries?)?\s*[\d\s()\-/]+(?:[A-Z]{2,8})?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public async Task<PsBookAnalysisResult> AnalyzeBookAsync(
        IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default)
    {
        ValidateFiles(files);

        var pages = new List<ParsedPsPage>();
        var warnings = new List<string>();

        for (var fileIndex = 0; fileIndex < files.Count; fileIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = files[fileIndex];
            var ps = await ReadPostScriptAsync(file, cancellationToken);

            var declaredPages = DetectDeclaredPageCount(ps);
            if (declaredPages is > 1)
            {
                warnings.Add(
                    $"{file.Name} declares {declaredPages} PostScript pages. This checker is optimized for the supplied format where each .ps file represents one book page.");
            }

            var pageLabel = DetectPageLabel(ps, fileIndex + 1);
            var names = ExtractListingNames(ps);

            if (names.Count == 0)
            {
                warnings.Add($"No listings were detected on page {pageLabel} ({file.Name}).");
            }

            pages.Add(new ParsedPsPage(
                FileName: file.Name,
                FileOrder: fileIndex + 1,
                PageLabel: pageLabel,
                ListingNames: names));
        }

        var listings = FlattenListings(pages);

        if (listings.Count == 0)
        {
            return new PsBookAnalysisResult
            {
                FilesAnalyzed = files.Count,
                PagesAnalyzed = pages.Count,
                ListingsAnalyzed = 0,
                Errors = [],
                Warnings = [.. warnings, "No valid listings could be identified in the uploaded book."]
            };
        }

        var errors = FindOutOfOrderListings(listings);

        return new PsBookAnalysisResult
        {
            FilesAnalyzed = files.Count,
            PagesAnalyzed = pages.Count,
            ListingsAnalyzed = listings.Count,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static void ValidateFiles(IReadOnlyList<IBrowserFile> files)
    {
        if (files.Count == 0)
            throw new InvalidOperationException("Select at least one .ps file.");

        if (files.Count > MaxFiles)
            throw new InvalidOperationException($"A maximum of {MaxFiles} .ps files can be analyzed at once.");

        foreach (var file in files)
        {
            if (!file.Name.EndsWith(".ps", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"'{file.Name}' is not a .ps file.");

            if (file.Size > MaxFileSize)
                throw new InvalidOperationException($"'{file.Name}' exceeds the 250 MB limit.");
        }
    }

    private static async Task<string> ReadPostScriptAsync(
        IBrowserFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream(MaxFileSize, cancellationToken);
        using var reader = new StreamReader(
            stream,
            Encoding.Latin1,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024,
            leaveOpen: false);

        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static int? DetectDeclaredPageCount(string ps)
    {
        var match = PagesCountRegex.Match(ps);
        return match.Success && int.TryParse(match.Groups[1].Value, out var count)
            ? count
            : null;
    }

    private static string DetectPageLabel(string ps, int fallbackPage)
    {
        // FOLIO is preferred because it is part of the AMDOCS page metadata.
        var folioMatches = PageFolioRegex.Matches(ps);
        if (folioMatches.Count > 0)
            return NormalizePageNumber(folioMatches[^1].Groups[1].Value);

        // PAGE:0006 appears in the production footer of the supplied files.
        var footerMatches = FooterPageRegex.Matches(ps);
        if (footerMatches.Count > 0)
            return NormalizePageNumber(footerMatches[^1].Groups[1].Value);

        return fallbackPage.ToString(CultureInfo.InvariantCulture);
    }

    private static string NormalizePageNumber(string raw)
    {
        return int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var page)
            ? page.ToString(CultureInfo.InvariantCulture)
            : raw.TrimStart('0');
    }

    private static List<string> ExtractListingNames(string ps)
    {
        var result = new List<string>();
        var fontMatches = FontRegex.Matches(ps);

        foreach (Match fontMatch in fontMatches)
        {
            if (!IsAtMainListingColumn(ps, fontMatch.Index))
                continue;

            if (IsPhoneOrNumberBlock(ps, fontMatch.Index))
                continue;

            var fontName = fontMatch.Groups[1].Value;
            var end = FindCandidateEnd(ps, fontMatch.Index + fontMatch.Length);

            if (end <= fontMatch.Index + fontMatch.Length)
                continue;

            var candidateSource = ps.Substring(
                fontMatch.Index + fontMatch.Length,
                end - (fontMatch.Index + fontMatch.Length));

            var text = ExtractPrintedText(candidateSource);
            text = CleanListingName(text);

            if (!IsLikelyListingName(text))
                continue;

            if (IsMastheadOrFooterText(text))
                continue;

            result.Add(text);
        }

        return result;
    }

    private static bool IsAtMainListingColumn(string ps, int fontIndex)
    {
        // In the supplied PS format, a real listing begins at the main text width: 126 cw.
        // Internal/detail lines are indented and normally use 121 cw.
        var contextStart = Math.Max(0, fontIndex - 650);
        var context = ps.Substring(contextStart, fontIndex - contextStart);
        var widths = ColumnWidthRegex.Matches(context);

        if (widths.Count == 0)
            return false;

        var lastWidth = widths[^1].Groups["width"].Value;
        return double.TryParse(lastWidth, NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
               && Math.Abs(width - 126.0) < 0.01;
    }

    private static bool IsPhoneOrNumberBlock(string ps, int fontIndex)
    {
        var contextStart = Math.Max(0, fontIndex - 260);
        var context = ps.Substring(contextStart, fontIndex - contextStart);
        var lastColumnWidth = context.LastIndexOf(" cw", StringComparison.OrdinalIgnoreCase);
        var lastSld = context.LastIndexOf("sld", StringComparison.OrdinalIgnoreCase);
        var lastSsw = context.LastIndexOf("ssw", StringComparison.OrdinalIgnoreCase);

        // Phone-number blocks in the supplied files are introduced by sld/ssw after
        // the listing's main 126-cw position has already been established.
        return lastSld >= 0 && lastSsw >= 0 && lastSld > lastColumnWidth && lastSsw > lastColumnWidth;
    }

    private static int FindCandidateEnd(string ps, int start)
    {
        var limit = Math.Min(ps.Length, start + 1800);
        var candidates = new List<int>();

        AddIndex(candidates, ps.IndexOf("121.00 cw", start, StringComparison.OrdinalIgnoreCase), start, limit);
        AddIndex(candidates, ps.IndexOf("126.00 cw", start, StringComparison.OrdinalIgnoreCase), start, limit);

        // A font change means the listing name has ended. This is particularly important
        // when address or phone information appears on the same physical line.
        var nextFont = AnyFontRegex.Match(ps, start);
        if (nextFont.Success)
            AddIndex(candidates, nextFont.Index, start, limit);

        return candidates.Count == 0 ? limit : candidates.Min();
    }

    private static void AddIndex(List<int> values, int index, int start, int limit)
    {
        if (index > start && index <= limit)
            values.Add(index);
    }

    private static string ExtractPrintedText(string source)
    {
        var builder = new StringBuilder();

        foreach (Match match in PsPrintedStringRegex.Matches(source))
        {
            var decoded = DecodePostScriptString(match.Groups["text"].Value);
            builder.Append(decoded);
        }

        return builder.ToString();
    }

    private static string DecodePostScriptString(string literal)
    {
        if (literal.Length < 2)
            return literal;

        var source = literal.AsSpan(1, literal.Length - 2);
        var builder = new StringBuilder(source.Length);

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];

            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            if (i + 1 >= source.Length)
                break;

            var next = source[++i];

            switch (next)
            {
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case '(':
                case ')':
                case '\\':
                    builder.Append(next);
                    break;
                case '\r':
                    if (i + 1 < source.Length && source[i + 1] == '\n')
                        i++;
                    break;
                case '\n':
                    break;
                default:
                    if (next is >= '0' and <= '7')
                    {
                        var octal = new StringBuilder(3);
                        octal.Append(next);

                        for (var j = 0; j < 2 && i + 1 < source.Length; j++)
                        {
                            var digit = source[i + 1];
                            if (digit is < '0' or > '7')
                                break;

                            i++;
                            octal.Append(digit);
                        }

                        if (Convert.ToInt32(octal.ToString(), 8) is var code)
                            builder.Append((char)code);
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

    private static string CleanListingName(string value)
    {
        var cleaned = value.Replace('\0', ' ');
        cleaned = MultipleWhitespaceRegex.Replace(cleaned, " ").Trim();
        cleaned = TrailingSeparatorRegex.Replace(cleaned, string.Empty).Trim();
        return cleaned;
    }

    private static bool IsLikelyListingName(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 1 || text.Length > 220)
            return false;

        // The PS structure already filters phone/detail blocks before this point.
        // Listings are therefore allowed to begin with numbers, for example
        // "1200 ..." or "2 ...". Those values are alphabetized by their
        // English number words in BuildSortKey.
        if (!text.Any(char.IsLetterOrDigit))
            return false;

        return true;
    }

    private static bool IsMastheadOrFooterText(string text)
    {
        var normalized = text.Trim().ToUpperInvariant();

        return normalized.Contains("WHITE PAGES DIRECTORY", StringComparison.Ordinal)
               || normalized.Contains("THRYV AUSTRALIA PTY LTD", StringComparison.Ordinal)
               || normalized.StartsWith("RHD - ", StringComparison.Ordinal)
               || normalized.StartsWith("CONFIDENTIAL AND PROPRIETARY", StringComparison.Ordinal)
               || normalized.StartsWith("PAGE:", StringComparison.Ordinal)
               || normalized == "2026";
    }

    private static List<PsListing> FlattenListings(IReadOnlyList<ParsedPsPage> pages)
    {
        var result = new List<PsListing>();
        var globalPosition = 0;

        foreach (var page in pages)
        {
            for (var index = 0; index < page.ListingNames.Count; index++)
            {
                globalPosition++;
                var name = page.ListingNames[index];

                result.Add(new PsListing
                {
                    Name = name,
                    SortKey = BuildSortKey(name),
                    FileName = page.FileName,
                    FileOrder = page.FileOrder,
                    PageOrderInFile = 1,
                    GlobalPageOrder = page.FileOrder,
                    PageLabel = page.PageLabel,
                    PositionInPage = index + 1,
                    GlobalPosition = globalPosition
                });
            }
        }

        return result;
    }

    private static string BuildSortKey(string value)
    {
        var valueForSorting = ReplaceLeadingNumberWithWords(value);
        var normalized = valueForSorting.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '/' or '&' or '\'' or '.' or ',')
            {
                builder.Append(' ');
            }
        }

        return MultipleWhitespaceRegex.Replace(builder.ToString(), " ").Trim();
    }

    private static string ReplaceLeadingNumberWithWords(string value)
    {
        var match = LeadingNumberRegex.Match(value);
        if (!match.Success)
            return value;

        var rawNumber = match.Groups["number"].Value.Replace(",", string.Empty, StringComparison.Ordinal);

        if (!ulong.TryParse(rawNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            // Extremely large values are still sorted alphabetically by spelling
            // each digit rather than leaving the numeric prefix in front.
            var digitWords = string.Join(' ', rawNumber.Select(DigitToWord));
            return digitWords + value[match.Length..];
        }

        var words = NumberToEnglishWords(number);
        return words + value[match.Length..];
    }

    private static string DigitToWord(char digit) => digit switch
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

    private static string NumberToEnglishWords(ulong number)
    {
        if (number == 0)
            return "zero";

        var scales = new (ulong Value, string Name)[]
        {
            (1_000_000_000_000_000_000UL, "quintillion"),
            (1_000_000_000_000_000UL, "quadrillion"),
            (1_000_000_000_000UL, "trillion"),
            (1_000_000_000UL, "billion"),
            (1_000_000UL, "million"),
            (1_000UL, "thousand")
        };

        var parts = new List<string>();
        var remaining = number;

        foreach (var (value, name) in scales)
        {
            if (remaining < value)
                continue;

            var group = remaining / value;
            remaining %= value;
            parts.Add($"{NumberBelowOneThousandToWords((int)group)} {name}");
        }

        if (remaining > 0)
            parts.Add(NumberBelowOneThousandToWords((int)remaining));

        return string.Join(' ', parts);
    }

    private static string NumberBelowOneThousandToWords(int number)
    {
        string[] ones =
        [
            "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
            "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
            "seventeen", "eighteen", "nineteen"
        ];

        string[] tens =
        [
            "", "", "twenty", "thirty", "forty", "fifty",
            "sixty", "seventy", "eighty", "ninety"
        ];

        var parts = new List<string>();

        if (number >= 100)
        {
            parts.Add($"{ones[number / 100]} hundred");
            number %= 100;
        }

        if (number >= 20)
        {
            parts.Add(tens[number / 10]);
            number %= 10;

            if (number > 0)
                parts.Add(ones[number]);
        }
        else if (number > 0)
        {
            parts.Add(ones[number]);
        }

        return string.Join(' ', parts);
    }

    private static List<PsListingOrderError> FindOutOfOrderListings(
        IReadOnlyList<PsListing> original)
    {
        if (original.Count < 2)
            return [];

        var errors = new List<PsListingOrderError>();
        var errorIds = new HashSet<Guid>();

        DetectForeignLetterRuns(
            original,
            errors,
            errorIds);

        DetectBackwardPrefixMovements(
            original,
            errors,
            errorIds);

        return errors
            .OrderBy(x => GetPageNumberForSorting(x.CurrentPage))
            .ThenBy(x => x.CurrentPositionInPage)
            .ThenBy(
                x => BuildSortKey(x.ListingName),
                StringComparer.Ordinal)
            .ToList();
    }

    private static void DetectForeignLetterRuns(
    IReadOnlyList<PsListing> listings,
    ICollection<PsListingOrderError> errors,
    ISet<Guid> errorIds)
    {
        if (listings.Count < 3)
            return;

        var index = 0;

        while (index < listings.Count)
        {
            var currentLetter =
                GetAlphabeticalLetter(
                    listings[index].Name);

            if (currentLetter == '\0')
            {
                index++;
                continue;
            }

            var runStart = index;
            var runEnd = index;

            while (runEnd + 1 < listings.Count &&
                   GetAlphabeticalLetter(
                       listings[runEnd + 1].Name) ==
                   currentLetter)
            {
                runEnd++;
            }

            if (runStart > 0 &&
                runEnd + 1 < listings.Count)
            {
                var previousLetter =
                    GetAlphabeticalLetter(
                        listings[runStart - 1].Name);

                var nextLetter =
                    GetAlphabeticalLetter(
                        listings[runEnd + 1].Name);

                if (previousLetter != '\0' &&
                    previousLetter == nextLetter &&
                    currentLetter != previousLetter)
                {
                    for (var i = runStart;
                         i <= runEnd;
                         i++)
                    {
                        AddStructuralError(
                            listings,
                            i,
                            errors,
                            errorIds);
                    }
                }
            }

            index = runEnd + 1;
        }
    }

    private static void DetectBackwardPrefixMovements(
    IReadOnlyList<PsListing> listings,
    ICollection<PsListingOrderError> errors,
    ISet<Guid> errorIds)
    {
        string? highestPrefix = null;
        char activeLetter = '\0';

        for (var i = 0; i < listings.Count; i++)
        {
            var item = listings[i];

            if (errorIds.Contains(item.Id))
                continue;

            var letter =
                GetAlphabeticalLetter(
                    item.Name);

            if (letter == '\0')
                continue;

            var prefix =
                GetSignificantAlphabeticalPrefix(
                    item.Name);

            if (string.IsNullOrWhiteSpace(prefix))
                continue;

            if (activeLetter == '\0' ||
                letter != activeLetter)
            {
                activeLetter = letter;
                highestPrefix = prefix;
                continue;
            }

            if (highestPrefix is not null &&
                string.Compare(
                    prefix,
                    highestPrefix,
                    StringComparison.Ordinal) < 0)
            {
                AddStructuralError(
                    listings,
                    i,
                    errors,
                    errorIds);
                continue;
            }

            if (highestPrefix is null ||
                string.Compare(
                    prefix,
                    highestPrefix,
                    StringComparison.Ordinal) > 0)
            {
                highestPrefix = prefix;
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

        foreach (var character in sortKey)
        {
            if (character is >= 'A' and <= 'Z')
                return character;
        }

        return '\0';
    }

    private static string GetSignificantAlphabeticalPrefix(
    string listingName)
    {
        var sortKey =
            BuildSortKey(
                listingName);

        if (string.IsNullOrWhiteSpace(
                sortKey))
        {
            return string.Empty;
        }

        var letters =
            new string(
                sortKey
                    .Where(char.IsLetter)
                    .Take(2)
                    .ToArray());

        return letters;
    }

    private static void AddStructuralError(
    IReadOnlyList<PsListing> listings,
    int currentIndex,
    ICollection<PsListingOrderError> errors,
    ISet<Guid> errorIds)
    {
        var item =
            listings[currentIndex];

        if (!errorIds.Add(item.Id))
            return;

        var correctPosition =
            FindRecommendedPosition(
                listings,
                item,
                currentIndex,
                errorIds);

        var target =
            listings[
                Math.Clamp(
                    correctPosition,
                    0,
                    listings.Count - 1)];

        PsListing? previous = null;
        PsListing? next = null;

        var itemSortKey =
            BuildSortKey(
                item.Name);

        /*
         * Buscamos el listing inmediatamente anterior
         * según el orden alfabético real.
         */
        for (var i = 0; i < listings.Count; i++)
        {
            if (i == currentIndex)
                continue;

            var candidate =
                listings[i];

            var candidateKey =
                BuildSortKey(
                    candidate.Name);

            if (string.Compare(
                    candidateKey,
                    itemSortKey,
                    StringComparison.Ordinal) <= 0)
            {
                if (previous is null ||
                    string.Compare(
                        candidateKey,
                        BuildSortKey(previous.Name),
                        StringComparison.Ordinal) > 0)
                {
                    previous = candidate;
                }
            }

            if (string.Compare(
                    candidateKey,
                    itemSortKey,
                    StringComparison.Ordinal) > 0)
            {
                if (next is null ||
                    string.Compare(
                        candidateKey,
                        BuildSortKey(next.Name),
                        StringComparison.Ordinal) < 0)
                {
                    next = candidate;
                }
            }
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
                    target.FileName,

                RecommendedPage =
                    target.PageLabel,

                RecommendedPositionInPage =
                    target.PositionInPage,

                ShouldGoAfter =
                    previous?.Name,

                ShouldGoBefore =
                    next?.Name
            });
    }

    private static int FindRecommendedPosition(
    IReadOnlyList<PsListing> listings,
    PsListing item,
    int currentIndex,
    ISet<Guid> ignoredErrorIds)
    {
        var itemKey =
            BuildSortKey(
                item.Name);

        for (var i = 0; i < listings.Count; i++)
        {
            if (i == currentIndex)
                continue;

            var candidate =
                listings[i];

            if (ignoredErrorIds.Contains(candidate.Id) &&
                candidate.Id != item.Id)
            {
                continue;
            }

            var candidateKey =
                BuildSortKey(
                    candidate.Name);

            if (string.Compare(
                    candidateKey,
                    itemKey,
                    StringComparison.Ordinal) > 0)
            {
                return i;
            }
        }

        return listings.Count - 1;
    }

    private static int GetPageNumberForSorting(
    string page)
    {
        return int.TryParse(
            page,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
                ? value
                : int.MaxValue;
    }

    private static HashSet<int> LongestIncreasingSubsequenceIndexes(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
            return [];

        var tails = new int[values.Count];
        var previous = Enumerable.Repeat(-1, values.Count).ToArray();
        var length = 0;

        for (var i = 0; i < values.Count; i++)
        {
            var low = 0;
            var high = length;

            while (low < high)
            {
                var middle = low + ((high - low) / 2);

                if (values[tails[middle]] < values[i])
                    low = middle + 1;
                else
                    high = middle;
            }

            if (low > 0)
                previous[i] = tails[low - 1];

            tails[low] = i;

            if (low == length)
                length++;
        }

        var indexes = new HashSet<int>();
        var cursor = tails[length - 1];

        while (cursor >= 0)
        {
            indexes.Add(cursor);
            cursor = previous[cursor];
        }

        return indexes;
    }

    private sealed record ParsedPsPage(
        string FileName,
        int FileOrder,
        string PageLabel,
        IReadOnlyList<string> ListingNames);
}
