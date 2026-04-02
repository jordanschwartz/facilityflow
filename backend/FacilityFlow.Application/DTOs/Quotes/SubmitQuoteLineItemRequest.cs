namespace FacilityFlow.Application.DTOs.Quotes;

public record SubmitQuoteLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice
);
