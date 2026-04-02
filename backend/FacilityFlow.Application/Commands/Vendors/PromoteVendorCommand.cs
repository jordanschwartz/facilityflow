using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Vendors;

public record PromoteVendorCommand(Guid Id) : IRequest<VendorDto>;

public class PromoteVendorCommandHandler : IRequestHandler<PromoteVendorCommand, VendorDto>
{
    private readonly IRepository<Vendor> _repo;

    public PromoteVendorCommandHandler(IRepository<Vendor> repo) => _repo = repo;

    public async Task<VendorDto> Handle(PromoteVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _repo.Query()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Vendor not found.");

        vendor.Status = VendorStatus.Active;

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
