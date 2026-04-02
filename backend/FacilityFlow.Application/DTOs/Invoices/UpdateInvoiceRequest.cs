namespace FacilityFlow.Application.DTOs.Invoices;

public record UpdateInvoiceRequest(decimal? Amount, string? Description, string? BillToName, string? BillToEmail, string? Notes);
