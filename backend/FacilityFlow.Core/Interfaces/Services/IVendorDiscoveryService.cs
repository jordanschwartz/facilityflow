using FacilityFlow.Core.Models;

namespace FacilityFlow.Core.Interfaces.Services;

public interface IVendorDiscoveryService
{
    Task<List<DiscoveredVendor>> SearchAsync(string trade, string zip, int radiusMiles);
}
