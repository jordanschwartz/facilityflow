using FacilityFlow.Core.Entities;

namespace FacilityFlow.Core.Interfaces.Repositories;

public interface IVendorRepository : IRepository<Vendor>
{
    Task<List<Vendor>> GetNearbyAsync(string zip, string? trade);
}
