using FacilityFlow.Application.DTOs.Common;

namespace FacilityFlow.Application.DTOs.Invoices;

public record InvoiceDto(
    Guid Id, Guid WorkOrderId, Guid ClientId,
    decimal Amount, string Description, string? Notes,
    string BillToName, string BillToEmail, string Location,
    string Status, string PublicToken,
    string? StripeInvoiceUrl,
    DateTime? SentAt, DateTime? PaidAt, DateTime CreatedAt,
    string? ServiceRequestTitle, string? VendorName,
    ClientSummaryDto? Client);
