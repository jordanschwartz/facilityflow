namespace FacilityFlow.Application.DTOs.VendorInvites;

public record CreateVendorInvitesResponse(List<VendorInviteDto> Created, List<Guid> Skipped);
