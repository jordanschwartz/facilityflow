using FacilityFlow.Core.Entities;

namespace FacilityFlow.Core.Interfaces.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetWithDetailsAsync(Guid id);
    Task<Invoice?> GetByWorkOrderIdAsync(Guid workOrderId);
}
