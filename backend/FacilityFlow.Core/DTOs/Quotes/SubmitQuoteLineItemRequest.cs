namespace FacilityFlow.Core.DTOs.Quotes;

public record SubmitQuoteLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice
);
