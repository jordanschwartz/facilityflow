using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Vendors;

public record GetNearbyVendorsQuery(string Zip, int RadiusMiles, string? Trade) : IRequest<List<VendorSourcingResultDto>>;

public class GetNearbyVendorsQueryHandler : IRequestHandler<GetNearbyVendorsQuery, List<VendorSourcingResultDto>>
{
    private readonly IRepository<Vendor> _vendorRepo;
    private readonly IRepository<WorkOrder> _workOrderRepo;
    private readonly IGeocodingService _geocodingService;

    public GetNearbyVendorsQueryHandler(
        IRepository<Vendor> vendorRepo,
        IRepository<WorkOrder> workOrderRepo,
        IGeocodingService geocodingService)
    {
        _vendorRepo = vendorRepo;
        _workOrderRepo = workOrderRepo;
        _geocodingService = geocodingService;
    }

    public async Task<List<VendorSourcingResultDto>> Handle(GetNearbyVendorsQuery request, CancellationToken cancellationToken)
    {
        var searchCoords = await _geocodingService.GeocodeZipAsync(request.Zip);
        if (searchCoords is null)
            return [];

        var (searchLat, searchLng) = searchCoords.Value;

        var query = _vendorRepo.Query()
            .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
            .Where(v => v.Status == VendorStatus.Active || v.Status == VendorStatus.Prospect);

        if (!string.IsNullOrWhiteSpace(request.Trade))
            query = query.Where(v => v.Trades.Contains(request.Trade));

        var vendors = await query.ToListAsync(cancellationToken);

        // Calculate Haversine distance in-memory and filter by radius
        var nearbyVendors = vendors
            .Select(v => new
            {
                Vendor = v,
                Distance = HaversineDistanceMiles(searchLat, searchLng, v.Latitude!.Value, v.Longitude!.Value)
            })
            .Where(x => x.Distance <= request.RadiusMiles && x.Distance <= x.Vendor.ServiceRadiusMiles)
            .OrderBy(x => x.Distance)
            .ToList();

        var vendorIds = nearbyVendors.Select(x => x.Vendor.Id).ToList();

        var workOrderStats = await _workOrderRepo.Query()
            .Where(wo => vendorIds.Contains(wo.VendorId))
            .GroupBy(wo => wo.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                CompletedJobCount = g.Count(wo => wo.Status == WorkOrderStatus.Completed),
                LastUsedDate = g.Max(wo => (DateTime?)wo.ServiceRequest.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var statsMap = workOrderStats.ToDictionary(s => s.VendorId);

        return nearbyVendors.Select(x =>
        {
            statsMap.TryGetValue(x.Vendor.Id, out var stats);
            return new VendorSourcingResultDto(
                x.Vendor.Id,
                x.Vendor.CompanyName,
                x.Vendor.PrimaryContactName,
                x.Vendor.Email,
                x.Vendor.PrimaryZip,
                x.Vendor.ServiceRadiusMiles,
                x.Vendor.Trades,
                x.Vendor.IsDnu,
                x.Vendor.DnuReason,
                stats?.CompletedJobCount ?? 0,
                stats?.LastUsedDate,
                Math.Round(x.Distance, 1)
            );
        }).ToList();
    }

    private static double HaversineDistanceMiles(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 3959; // Earth radius in miles

        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
