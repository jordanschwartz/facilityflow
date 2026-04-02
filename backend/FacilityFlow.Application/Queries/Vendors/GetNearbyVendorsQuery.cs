using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Vendors;

public record GetNearbyVendorsQuery(string Zip, int RadiusMiles, string? Trade) : IRequest<List<VendorSourcingResultDto>>;

public class GetNearbyVendorsQueryHandler : IRequestHandler<GetNearbyVendorsQuery, List<VendorSourcingResultDto>>
{
    private readonly IVendorRepository _vendorRepo;
    private readonly IRepository<WorkOrder> _workOrderRepo;

    public GetNearbyVendorsQueryHandler(IVendorRepository vendorRepo, IRepository<WorkOrder> workOrderRepo)
    {
        _vendorRepo = vendorRepo;
        _workOrderRepo = workOrderRepo;
    }

    public async Task<List<VendorSourcingResultDto>> Handle(GetNearbyVendorsQuery request, CancellationToken cancellationToken)
    {
        var vendors = await _vendorRepo.GetNearbyAsync(request.Zip, request.Trade);

        var vendorIds = vendors.Select(v => v.Id).ToList();

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

        return vendors.Select(v =>
        {
            statsMap.TryGetValue(v.Id, out var stats);
            return new VendorSourcingResultDto(
                v.Id,
                v.CompanyName,
                v.PrimaryContactName,
                v.Email,
                v.PrimaryZip,
                v.ServiceRadiusMiles,
                v.Trades,
                v.IsDnu,
                v.DnuReason,
                stats?.CompletedJobCount ?? 0,
                stats?.LastUsedDate
            );
        }).ToList();
    }
}
