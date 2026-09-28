using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Snowwave.ManifestIngestion;

public sealed class ManifestParseException : Exception
{
    public ManifestParseException(string message) : base(message) { }
    public ManifestParseException(string message, Exception inner) : base(message, inner) { }
}

public sealed record ManifestCsvRow(
    int RowNumber,
    string ParcelId,
    string? CustomerName,
    string? AddressLine1,
    string? UnitNumber,
    string? City,
    string? PostalCode,
    string? CustomerPhone,
    string? Notes,
    string? CustomerEmail,
    string? BuzzerCode,
    string? OrderReference);

public sealed record ManifestCsvRowError(
    int RowNumber,
    string RawRow,
    string ErrorCode,
    string ErrorMessage,
    string Classification);

public sealed record ManifestCsvParseResult(
    IReadOnlyList<ManifestCsvRow> ValidRows,
    IReadOnlyList<ManifestCsvRowError> RejectedRows,
    int TotalRows);

/// <summary>
/// Server-side CSV parser from Snowwave's manifest ingestion path.
/// It normalizes headers, supports aliases, handles quoted fields, and fails
/// loudly on malformed source data instead of silently producing bad rows.
/// </summary>
public static class ManifestCsvParser
{
    private const int MaxRawRowLength = 4096;

    public static ManifestCsvParseResult Parse(Stream csv)
    {
        using var sourceReader = new StreamReader(csv);
        var content = sourceReader.ReadToEnd();
        if (content.Count(c => c == '"') % 2 != 0)
        {
            throw new ManifestParseException("malformed CSV: unbalanced quote characters");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            LineBreakInQuotedFieldIsBadData = true,
            TrimOptions = TrimOptions.Trim,
        };

        using var reader = new StringReader(content);
        using var parser = new CsvReader(reader, config);

        if (!parser.Read() || !parser.ReadHeader() || parser.HeaderRecord is null || parser.HeaderRecord.Length == 0)
        {
            throw new ManifestParseException("manifest has no header row");
        }

        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < parser.HeaderRecord.Length; i++)
        {
            var key = new string(parser.HeaderRecord[i].ToLowerInvariant().Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (key.Length > 0 && !index.ContainsKey(key))
            {
                index[key] = i;
            }
        }

        if (!index.ContainsKey("parcelid"))
        {
            throw new ManifestParseException("manifest is missing the required 'parcelId' column");
        }

        string? Get(params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                if (index.TryGetValue(alias, out var i) && parser.TryGetField<string>(i, out var value))
                {
                    var trimmed = value?.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        if (trimmed.Contains('\n') || trimmed.Contains('\r'))
                        {
                            throw new ManifestParseException(
                                $"malformed CSV near row {parser.Parser.Row}: embedded newline in field");
                        }
                        return trimmed;
                    }
                }
            }
            return null;
        }

        var rows = new List<ManifestCsvRow>();
        var rejectedRows = new List<ManifestCsvRowError>();
        var firstRowByParcelId = new Dictionary<string, int>(StringComparer.Ordinal);
        var rowNumber = 0;

        while (ReadOrFailLoud(parser))
        {
            rowNumber++;
            var rawRow = TruncateRawRow(parser.Parser.RawRecord);
            var parcelId = Get("parcelid");
            if (parcelId is null)
            {
                rejectedRows.Add(new ManifestCsvRowError(
                    rowNumber, rawRow, "MISSING_PARCEL_ID",
                    "missing required parcelId in canonical row", "CANONICAL_ROW_INVALID"));
                continue;
            }

            if (firstRowByParcelId.TryGetValue(parcelId, out var winningRow))
            {
                rejectedRows.Add(new ManifestCsvRowError(
                    rowNumber, rawRow, "DUPLICATE_PARCEL_ID_IN_FILE",
                    $"duplicate parcelId '{parcelId}' in file; first occurrence is row {winningRow}",
                    "CANONICAL_ROW_INVALID"));
                continue;
            }

            firstRowByParcelId[parcelId] = rowNumber;
            rows.Add(new ManifestCsvRow(
                rowNumber,
                parcelId,
                Get("customername"),
                Get("addressline1", "address"),
                Get("unitnumber", "unit"),
                Get("city"),
                Get("postalcode", "postal"),
                Get("customerphone", "phone"),
                Get("notes", "deliveryinstructions"),
                Get("customeremail", "email"),
                Get("buzzercode", "buzzer"),
                Get("orderreference", "order")));
        }

        return new ManifestCsvParseResult(rows, rejectedRows, rowNumber);
    }

    private static string TruncateRawRow(string? rawRow)
    {
        var value = rawRow ?? string.Empty;
        return value.Length <= MaxRawRowLength ? value : value[..MaxRawRowLength];
    }

    private static bool ReadOrFailLoud(CsvReader parser)
    {
        try
        {
            return parser.Read();
        }
        catch (CsvHelper.BadDataException ex)
        {
            throw new ManifestParseException(
                $"malformed CSV near row {ex.Context?.Parser?.Row}: bad field quoting", ex);
        }
    }
}
