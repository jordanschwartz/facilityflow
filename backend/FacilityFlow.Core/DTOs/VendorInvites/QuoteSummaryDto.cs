namespace FacilityFlow.Core.DTOs.VendorInvites;

public record QuoteSummaryDto(Guid Id, string Status, decimal? Price, DateTime? SubmittedAt);
