using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Repositories;

public class VendorRepository : Repository<Vendor>, IVendorRepository
{
    public VendorRepository(AppDbContext db) : base(db) { }

    public async Task<List<Vendor>> GetNearbyAsync(string zip, string? trade)
    {
        var zipPrefix = zip.Length >= 3 ? zip[..3] : zip;

        var query = DbSet
            .Where(v => v.IsActive && (v.PrimaryZip == zip || v.PrimaryZip.StartsWith(zipPrefix)));

        if (!string.IsNullOrWhiteSpace(trade))
            query = query.Where(v => v.Trades.Contains(trade));

        return await query.ToListAsync();
    }
}
