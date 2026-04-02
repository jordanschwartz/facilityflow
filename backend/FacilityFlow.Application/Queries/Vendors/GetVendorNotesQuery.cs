using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Vendors;

public record GetVendorNotesQuery(Guid VendorId) : IRequest<List<VendorNoteDto>>;

public class GetVendorNotesQueryHandler : IRequestHandler<GetVendorNotesQuery, List<VendorNoteDto>>
{
    private readonly IRepository<Vendor> _vendorRepo;
    private readonly IRepository<VendorNote> _noteRepo;

    public GetVendorNotesQueryHandler(IRepository<Vendor> vendorRepo, IRepository<VendorNote> noteRepo)
    {
        _vendorRepo = vendorRepo;
        _noteRepo = noteRepo;
    }

    public async Task<List<VendorNoteDto>> Handle(GetVendorNotesQuery request, CancellationToken cancellationToken)
    {
        if (!await _vendorRepo.ExistsAsync(request.VendorId))
            throw new NotFoundException("Vendor not found.");

        var notes = await _noteRepo.Query()
            .Include(n => n.CreatedBy)
            .Where(n => n.VendorId == request.VendorId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(n => new VendorNoteDto(
            n.Id,
            n.VendorId,
            n.Text,
            n.AttachmentUrl,
            n.AttachmentFilename,
            n.CreatedBy.Name,
            n.CreatedAt
        )).ToList();
    }
}
