using System.Text.Json.Serialization;

namespace Snowwave.ManifestIngestion;

public sealed record ManifestRowError(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("batchId")] string BatchId,
    [property: JsonPropertyName("retailerId")] string RetailerId,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("manifestRowNumber")] int ManifestRowNumber,
    [property: JsonPropertyName("rawRow")] string RawRow,
    [property: JsonPropertyName("errorCode")] string ErrorCode,
    [property: JsonPropertyName("errorMessage")] string ErrorMessage,
    [property: JsonPropertyName("classification")] string Classification,
    [property: JsonPropertyName("createdAtUtc")] DateTimeOffset CreatedAtUtc);
