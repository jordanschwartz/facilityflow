using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Vendors;

public record CreateVendorCommand(CreateVendorRequest Request) : IRequest<VendorDto>;

public class CreateVendorCommandHandler : IRequestHandler<CreateVendorCommand, VendorDto>
{
    private readonly IRepository<Vendor> _repo;
    private readonly IGeocodingService _geocodingService;

    public CreateVendorCommandHandler(IRepository<Vendor> repo, IGeocodingService geocodingService)
    {
        _repo = repo;
        _geocodingService = geocodingService;
    }

    public async Task<VendorDto> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            CompanyName = req.CompanyName,
            PrimaryContactName = req.PrimaryContactName,
            Email = req.Email,
            Phone = req.Phone ?? string.Empty,
            PrimaryZip = req.PrimaryZip.Trim(),
            ServiceRadiusMiles = req.ServiceRadiusMiles,
            Trades = req.Trades ?? [],
            ZipCodes = req.ZipCodes ?? [],
            IsActive = req.IsActive,
            IsDnu = req.IsDnu,
            DnuReason = req.DnuReason
        };

        var coords = await _geocodingService.GeocodeZipAsync(vendor.PrimaryZip);
        if (coords.HasValue)
        {
            vendor.Latitude = coords.Value.Latitude;
            vendor.Longitude = coords.Value.Longitude;
        }

        _repo.Add(vendor);
        await _repo.SaveChangesAsync();

        // Reload with User nav if UserId provided
        if (vendor.UserId.HasValue)
        {
            vendor = await _repo.Query()
                .Include(v => v.User)
                .FirstAsync(v => v.Id == vendor.Id, cancellationToken);
        }

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
