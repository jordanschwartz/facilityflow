using FacilityFlow.Core.Entities;

namespace FacilityFlow.Core.Interfaces.Repositories;

public interface IServiceRequestRepository : IRepository<ServiceRequest>
{
    Task<ServiceRequest?> GetWithDetailsAsync(Guid id);
    Task<ServiceRequest?> GetWithInvitesAsync(Guid id);
}
