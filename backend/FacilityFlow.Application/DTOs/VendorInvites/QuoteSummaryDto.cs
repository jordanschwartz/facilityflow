namespace FacilityFlow.Application.DTOs.VendorInvites;

public record QuoteSummaryDto(Guid Id, string Status, decimal? Price, DateTime? SubmittedAt);
