using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Vendors;

public record DiscoverVendorsQuery(string Trade, string Zip, int RadiusMiles = 25) : IRequest<List<DiscoveredVendorDto>>;

public class DiscoverVendorsQueryHandler : IRequestHandler<DiscoverVendorsQuery, List<DiscoveredVendorDto>>
{
    private readonly IVendorDiscoveryService _discoveryService;
    private readonly IRepository<Vendor> _vendorRepo;

    public DiscoverVendorsQueryHandler(
        IVendorDiscoveryService discoveryService,
        IRepository<Vendor> vendorRepo)
    {
        _discoveryService = discoveryService;
        _vendorRepo = vendorRepo;
    }

    public async Task<List<DiscoveredVendorDto>> Handle(DiscoverVendorsQuery request, CancellationToken cancellationToken)
    {
        var discovered = await _discoveryService.SearchAsync(request.Trade, request.Zip, request.RadiusMiles);

        var existingVendors = await _vendorRepo.Query()
            .Select(v => new { v.Id, v.CompanyName })
            .ToListAsync(cancellationToken);

        return discovered.Select(d =>
        {
            var match = existingVendors.FirstOrDefault(v =>
                v.CompanyName.Contains(d.BusinessName, StringComparison.OrdinalIgnoreCase) ||
                d.BusinessName.Contains(v.CompanyName, StringComparison.OrdinalIgnoreCase));

            return new DiscoveredVendorDto(
                d.BusinessName,
                d.Address,
                d.Phone,
                d.Website,
                d.Rating,
                d.ReviewCount,
                d.GoogleProfileUrl,
                match?.Id);
        }).ToList();
    }
}
