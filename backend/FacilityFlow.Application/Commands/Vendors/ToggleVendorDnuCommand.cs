using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Vendors;

public record ToggleVendorDnuCommand(Guid Id, ToggleDnuRequest Request) : IRequest<VendorDto>;

public class ToggleVendorDnuCommandHandler : IRequestHandler<ToggleVendorDnuCommand, VendorDto>
{
    private readonly IRepository<Vendor> _repo;

    public ToggleVendorDnuCommandHandler(IRepository<Vendor> repo) => _repo = repo;

    public async Task<VendorDto> Handle(ToggleVendorDnuCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repo.Query()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Vendor not found.");

        vendor.IsDnu = request.Request.IsDnu;
        vendor.DnuReason = request.Request.IsDnu ? request.Request.Reason : null;

        await _repo.SaveChangesAsync();
        return ToDto(vendor);
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
        v.Latitude,
        v.Longitude,
        v.User?.Adapt<UserDto>());
}
