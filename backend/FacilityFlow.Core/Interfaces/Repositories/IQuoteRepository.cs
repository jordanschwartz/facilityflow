using FacilityFlow.Core.Entities;

namespace FacilityFlow.Core.Interfaces.Repositories;

public interface IQuoteRepository : IRepository<Quote>
{
    Task<Quote?> GetByTokenWithDetailsAsync(string token);
    Task<List<Quote>> GetByServiceRequestIdAsync(Guid serviceRequestId);
}
