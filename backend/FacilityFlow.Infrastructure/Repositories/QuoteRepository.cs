using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Repositories;

public class QuoteRepository : Repository<Quote>, IQuoteRepository
{
    public QuoteRepository(AppDbContext db) : base(db) { }

    public async Task<Quote?> GetByTokenWithDetailsAsync(string token)
    {
        return await DbSet
            .Include(q => q.ServiceRequest)
            .Include(q => q.Vendor).ThenInclude(v => v.User)
            .Include(q => q.Attachments)
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.PublicToken == token);
    }

    public async Task<List<Quote>> GetByServiceRequestIdAsync(Guid serviceRequestId)
    {
        return await DbSet
            .Include(q => q.Vendor).ThenInclude(v => v.User)
            .Include(q => q.Attachments)
            .Include(q => q.LineItems)
            .Where(q => q.ServiceRequestId == serviceRequestId)
            .ToListAsync();
    }
}
