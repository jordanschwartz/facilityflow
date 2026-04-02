namespace FacilityFlow.Application.DTOs.Quotes;

public record SubmitQuoteRequest(
    decimal Price,
    string ScopeOfWork,
    DateTime? ProposedStartDate = null,
    int? EstimatedDurationValue = null,
    string? EstimatedDurationUnit = null,
    decimal? NotToExceedPrice = null,
    string? Assumptions = null,
    string? Exclusions = null,
    string? VendorAvailability = null,
    DateTime? ValidUntil = null,
    List<SubmitQuoteLineItemRequest>? LineItems = null
);
