namespace FacilityFlow.Application.DTOs.Quotes;

public record QuoteLineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total
);
