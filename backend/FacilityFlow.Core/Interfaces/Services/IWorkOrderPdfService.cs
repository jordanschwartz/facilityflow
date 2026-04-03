namespace FacilityFlow.Core.Interfaces.Services;

public interface IWorkOrderPdfService
{
    Task<byte[]> GeneratePdfAsync(Guid serviceRequestId, Guid vendorInviteId);
}
