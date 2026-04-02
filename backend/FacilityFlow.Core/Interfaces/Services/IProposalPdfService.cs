namespace FacilityFlow.Core.Interfaces.Services;

public interface IProposalPdfService
{
    Task<byte[]> GenerateAsync(Guid proposalId);
}
