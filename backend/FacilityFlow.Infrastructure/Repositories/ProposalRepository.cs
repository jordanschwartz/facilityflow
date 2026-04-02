using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Repositories;

public class ProposalRepository : Repository<Proposal>, IProposalRepository
{
    public ProposalRepository(AppDbContext db) : base(db) { }

    public async Task<Proposal?> GetWithFullDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
            .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Quotes)
            .Include(p => p.ServiceRequest).ThenInclude(sr => sr.WorkOrder)
            .Include(p => p.Quote)
            .Include(p => p.Attachments).ThenInclude(pa => pa.Attachment)
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Proposal?> GetByTokenAsync(string token)
    {
        return await DbSet
            .Include(p => p.ServiceRequest)
            .Include(p => p.Attachments).ThenInclude(pa => pa.Attachment)
            .FirstOrDefaultAsync(p => p.PublicToken == token);
    }

    public async Task<Proposal?> GetByServiceRequestIdAsync(Guid serviceRequestId)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.ServiceRequestId == serviceRequestId);
    }
}
