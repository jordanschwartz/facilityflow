namespace FacilityFlow.Application.DTOs.Invoices;

public record CreateInvoiceRequest(decimal Amount, string Description, string BillToName, string BillToEmail, string? Notes);
