using FacilityFlow.Core.Entities;

namespace FacilityFlow.Core.Interfaces.Repositories;

public interface IProposalRepository : IRepository<Proposal>
{
    Task<Proposal?> GetWithFullDetailsAsync(Guid id);
    Task<Proposal?> GetByTokenAsync(string token);
    Task<Proposal?> GetByServiceRequestIdAsync(Guid serviceRequestId);
}
