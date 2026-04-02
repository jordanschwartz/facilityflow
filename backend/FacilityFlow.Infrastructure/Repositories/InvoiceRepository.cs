using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext db) : base(db) { }

    public async Task<Invoice?> GetWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(i => i.WorkOrder).ThenInclude(wo => wo.ServiceRequest).ThenInclude(sr => sr.Client).ThenInclude(c => c.User)
            .Include(i => i.WorkOrder).ThenInclude(wo => wo.Proposal)
            .Include(i => i.WorkOrder).ThenInclude(wo => wo.Vendor)
            .Include(i => i.Client).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice?> GetByWorkOrderIdAsync(Guid workOrderId)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.WorkOrderId == workOrderId);
    }
}
