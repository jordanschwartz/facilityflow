using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Vendors;

public record UpdateVendorCommand(Guid Id, UpdateVendorRequest Request) : IRequest<VendorDto>;

public class UpdateVendorCommandHandler : IRequestHandler<UpdateVendorCommand, VendorDto>
{
    private readonly IRepository<Vendor> _repo;
    private readonly IGeocodingService _geocodingService;

    public UpdateVendorCommandHandler(IRepository<Vendor> repo, IGeocodingService geocodingService)
    {
        _repo = repo;
        _geocodingService = geocodingService;
    }

    public async Task<VendorDto> Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repo.Query()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Vendor not found.");

        var req = request.Request;
        var previousZip = vendor.PrimaryZip;

        vendor.CompanyName = req.CompanyName;
        vendor.PrimaryContactName = req.PrimaryContactName;
        vendor.Email = req.Email;
        vendor.Phone = req.Phone ?? string.Empty;
        vendor.PrimaryZip = req.PrimaryZip.Trim();
        vendor.ServiceRadiusMiles = req.ServiceRadiusMiles;
        vendor.Trades = req.Trades ?? vendor.Trades;
        vendor.ZipCodes = req.ZipCodes ?? vendor.ZipCodes;
        vendor.IsActive = req.IsActive;

        if (vendor.PrimaryZip != previousZip)
        {
            var coords = await _geocodingService.GeocodeZipAsync(vendor.PrimaryZip);
            if (coords.HasValue)
            {
                vendor.Latitude = coords.Value.Latitude;
                vendor.Longitude = coords.Value.Longitude;
            }
        }

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
