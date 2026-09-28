using System.Text.Json.Serialization;

namespace Snowwave.ManifestIngestion;

// This public slice keeps only the event fields required by manifest ingestion.
// The private application contains a larger contract surface.
public sealed record ParcelIngestParcelRequest(
    [property: JsonPropertyName("parcelId")] string? ParcelId,
    [property: JsonPropertyName("recipientName")] string? RecipientName,
    [property: JsonPropertyName("addressLine1")] string? AddressLine1,
    [property: JsonPropertyName("addressLine2")] string? AddressLine2,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("serviceLevel")] string? ServiceLevel,
    [property: JsonPropertyName("customerPhone")] string? CustomerPhone,
    [property: JsonPropertyName("deliveryInstructions")] string? DeliveryInstructions,
    [property: JsonPropertyName("customerEmail")] string? CustomerEmail,
    [property: JsonPropertyName("buzzerCode")] string? BuzzerCode = null);

public sealed record ParcelIngestedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("batchId")] string? BatchId,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("parcel")] ParcelIngestParcelRequest? Parcel,
    [property: JsonPropertyName("sourceSystem")] string SourceSystem,
    [property: JsonPropertyName("createdAtUtc")] DateTimeOffset CreatedAtUtc,
    [property: JsonPropertyName("routeId")] string? RouteId = null,
    [property: JsonPropertyName("orderReference")] string? OrderReference = null,
    [property: JsonPropertyName("manifestRowNumber")] int? ManifestRowNumber = null,
    [property: JsonPropertyName("routeDate")] string? RouteDate = null);
