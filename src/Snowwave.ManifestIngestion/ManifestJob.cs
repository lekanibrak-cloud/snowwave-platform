using System.Text.Json.Serialization;

namespace Snowwave.ManifestIngestion;

/// <summary>
/// Durable processing ledger for one uploaded manifest.
/// The blob is the source artifact; this document records processing state.
/// Partition key: /tenantId.
/// </summary>
public sealed record ManifestJob(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("retailerId")] string RetailerId,
    [property: JsonPropertyName("blobPath")] string BlobPath,
    [property: JsonPropertyName("originalFileName")] string OriginalFileName,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("attemptCount")] int AttemptCount,
    [property: JsonPropertyName("lastError")] string? LastError,
    [property: JsonPropertyName("createdAtUtc")] DateTimeOffset CreatedAtUtc,
    [property: JsonPropertyName("lastTransitionAtUtc")] DateTimeOffset LastTransitionAtUtc,
    [property: JsonPropertyName("uploadedBy")] string? UploadedBy,
    [property: JsonPropertyName("routeId")] string? RouteId,
    [property: JsonPropertyName("routeDate")] string? RouteDate,
    [property: JsonPropertyName("rowsParsed")] int RowsParsed,
    [property: JsonPropertyName("rowsRejected")] int RowsRejected,
    [property: JsonPropertyName("rowsPublished")] int RowsPublished,
    [property: JsonPropertyName("lastPublishedRow")] int LastPublishedRow,
    [property: JsonPropertyName("totalRows")] int? TotalRows,
    [property: JsonPropertyName("ownerInstanceId")] string? OwnerInstanceId,
    [property: JsonPropertyName("claimedAtUtc")] DateTimeOffset? ClaimedAtUtc,
    [property: JsonPropertyName("nextAttemptAtUtc")] DateTimeOffset? NextAttemptAtUtc = null);
