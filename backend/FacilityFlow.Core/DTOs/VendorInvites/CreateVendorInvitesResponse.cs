namespace FacilityFlow.Core.DTOs.VendorInvites;

public record CreateVendorInvitesResponse(List<VendorInviteDto> Created, List<Guid> Skipped);
