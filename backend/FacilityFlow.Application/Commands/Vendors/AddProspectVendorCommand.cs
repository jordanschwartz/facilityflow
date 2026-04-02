using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Vendors;

public record AddProspectVendorCommand(AddProspectVendorRequest Request) : IRequest<VendorDto>;

public class AddProspectVendorCommandHandler : IRequestHandler<AddProspectVendorCommand, VendorDto>
{
    private readonly IRepository<Vendor> _repo;

    public AddProspectVendorCommandHandler(IRepository<Vendor> repo) => _repo = repo;

    public async Task<VendorDto> Handle(AddProspectVendorCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var duplicate = await _repo.Query()
            .AnyAsync(v => v.CompanyName.ToLower() == req.CompanyName.ToLower(), cancellationToken);

        if (duplicate)
            throw new InvalidOperationException($"A vendor with the name '{req.CompanyName}' already exists.");

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            CompanyName = req.CompanyName,
            PrimaryContactName = req.PrimaryContactName ?? string.Empty,
            Email = req.Email ?? string.Empty,
            Phone = req.Phone ?? string.Empty,
            PrimaryZip = req.PrimaryZip.Trim(),
            Trades = req.Trades ?? [],
            Status = VendorStatus.Prospect,
            IsActive = true,
            IsDnu = false,
            Rating = req.Rating,
            ReviewCount = req.ReviewCount,
            Website = req.Website,
            GoogleProfileUrl = req.GoogleProfileUrl
        };

        _repo.Add(vendor);
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
        v.User?.Adapt<UserDto>());
}
