namespace FacilityFlow.Application.DTOs.Invoices;

public record BillableWorkOrderDto(
    Guid Id, Guid ServiceRequestId,
    string Title, string Location, string ClientName, string ClientEmail,
    DateTime? CompletedAt, decimal? ProposalAmount, string? ScopeOfWork);
