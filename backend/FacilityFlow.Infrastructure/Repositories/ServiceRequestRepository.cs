using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Repositories;

public class ServiceRequestRepository : Repository<ServiceRequest>, IServiceRequestRepository
{
    public ServiceRequestRepository(AppDbContext db) : base(db) { }

    public async Task<ServiceRequest?> GetWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(s => s.Client).ThenInclude(c => c.User)
            .Include(s => s.CreatedBy)
            .Include(s => s.Quotes)
            .Include(s => s.Proposal)
            .Include(s => s.WorkOrder)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<ServiceRequest?> GetWithInvitesAsync(Guid id)
    {
        return await DbSet
            .Include(s => s.VendorInvites)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}
