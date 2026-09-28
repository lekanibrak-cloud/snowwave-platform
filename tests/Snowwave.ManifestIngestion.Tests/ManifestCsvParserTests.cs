using System.Text;
using Snowwave.ManifestIngestion;

namespace Snowwave.ManifestIngestion.Tests;

public sealed class ManifestCsvParserTests
{
    private static Stream Csv(string value) => new MemoryStream(Encoding.UTF8.GetBytes(value));

    [Fact]
    public void Parse_HandlesQuotedFieldsAndAliases()
    {
        var input = "parcelid,address,postal,customername\nP1,\"100 King St W, Suite 200\",M5V1A1,\"Doe, Jane\"\n";
        var result = ManifestCsvParser.Parse(Csv(input));

        var row = Assert.Single(result.ValidRows);
        Assert.Equal("P1", row.ParcelId);
        Assert.Equal("100 King St W, Suite 200", row.AddressLine1);
        Assert.Equal("Doe, Jane", row.CustomerName);
    }

    [Fact]
    public void Parse_RejectsMissingParcelIdWithoutLosingRowNumber()
    {
        var input = "parcelId,customerName\nP1,Alice\n,NoId\nP3,Carol\n";
        var result = ManifestCsvParser.Parse(Csv(input));

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.ValidRows.Count);
        var error = Assert.Single(result.RejectedRows);
        Assert.Equal(2, error.RowNumber);
        Assert.Equal("MISSING_PARCEL_ID", error.ErrorCode);
        Assert.Equal(3, result.ValidRows[1].RowNumber);
    }

    [Fact]
    public void Parse_RejectsDuplicateParcelId_FirstRowWins()
    {
        var input = "parcelId,customerName\nP1,Alice\nP1,Alice-Dupe\n";
        var result = ManifestCsvParser.Parse(Csv(input));

        Assert.Single(result.ValidRows);
        var error = Assert.Single(result.RejectedRows);
        Assert.Equal("DUPLICATE_PARCEL_ID_IN_FILE", error.ErrorCode);
        Assert.Contains("first occurrence is row 1", error.ErrorMessage);
    }

    [Fact]
    public void Parse_MissingParcelIdColumn_FailsLoud()
    {
        var ex = Assert.Throws<ManifestParseException>(() =>
            ManifestCsvParser.Parse(Csv("name,city\nAlice,Toronto\n")));

        Assert.Contains("required 'parcelId' column", ex.Message);
    }
}
