namespace FacilityFlow.Application.DTOs.Invoices;

public record InvoiceSummaryDto(
    Guid Id, Guid WorkOrderId,
    string ClientName, string Location,
    DateTime? CompletedAt, decimal Amount,
    string Status, DateTime? SentAt, DateTime? PaidAt,
    string BillToEmail);
