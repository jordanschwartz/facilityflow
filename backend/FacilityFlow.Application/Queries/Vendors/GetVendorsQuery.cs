using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Vendors;

public record GetVendorsQuery(
    string? Trade,
    string? Zip,
    string? Search,
    bool? IsActive,
    bool? IsDnu,
    int Page,
    int PageSize) : IRequest<PagedResult<VendorDto>>;

public class GetVendorsQueryHandler : IRequestHandler<GetVendorsQuery, PagedResult<VendorDto>>
{
    private readonly IRepository<Vendor> _repo;

    public GetVendorsQueryHandler(IRepository<Vendor> repo) => _repo = repo;

    public async Task<PagedResult<VendorDto>> Handle(GetVendorsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query().Include(v => v.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Trade))
            query = query.Where(v => v.Trades.Contains(request.Trade));

        if (!string.IsNullOrWhiteSpace(request.Zip))
            query = query.Where(v => v.ZipCodes.Contains(request.Zip) || v.PrimaryZip == request.Zip);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(v => v.CompanyName.ToLower().Contains(request.Search.ToLower()));

        if (request.IsActive.HasValue)
            query = query.Where(v => v.IsActive == request.IsActive.Value);

        if (request.IsDnu.HasValue)
            query = query.Where(v => v.IsDnu == request.IsDnu.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(v => v.CompanyName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(ToDto).ToList();
        return new PagedResult<VendorDto>(dtos, total, request.Page, request.PageSize);
    }

    private static VendorDto ToDto(Vendor v) => new(
        v.Id,
        v.UserId,
        v.CompanyName,
        v.PrimaryContactName,
        v.Email,
        v.Phone,
        v.PrimaryZip,
        v.ServiceRadiusMiles,
        v.Trades,
        v.ZipCodes,
        v.Rating,
        v.IsActive,
        v.IsDnu,
        v.DnuReason,
        v.Status.ToString(),
        v.Website,
        v.ReviewCount,
        v.GoogleProfileUrl,
        v.User?.Adapt<UserDto>());
}
