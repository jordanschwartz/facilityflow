using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Vendors;

public record CreateVendorNoteCommand(Guid VendorId, CreateVendorNoteRequest Request, Guid UserId) : IRequest<VendorNoteDto>;

public class CreateVendorNoteCommandHandler : IRequestHandler<CreateVendorNoteCommand, VendorNoteDto>
{
    private readonly IRepository<Vendor> _vendorRepo;
    private readonly IRepository<VendorNote> _noteRepo;
    private readonly IRepository<User> _userRepo;

    public CreateVendorNoteCommandHandler(
        IRepository<Vendor> vendorRepo,
        IRepository<VendorNote> noteRepo,
        IRepository<User> userRepo)
    {
        _vendorRepo = vendorRepo;
        _noteRepo = noteRepo;
        _userRepo = userRepo;
    }

    public async Task<VendorNoteDto> Handle(CreateVendorNoteCommand request, CancellationToken cancellationToken)
    {
        if (!await _vendorRepo.ExistsAsync(request.VendorId))
            throw new NotFoundException("Vendor not found.");

        var note = new VendorNote
        {
            Id = Guid.NewGuid(),
            VendorId = request.VendorId,
            Text = request.Request.Text,
            AttachmentUrl = request.Request.AttachmentUrl,
            AttachmentFilename = request.Request.AttachmentFilename,
            CreatedById = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _noteRepo.Add(note);
        await _noteRepo.SaveChangesAsync();

        var creator = await _userRepo.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.");

        return new VendorNoteDto(
            note.Id,
            note.VendorId,
            note.Text,
            note.AttachmentUrl,
            note.AttachmentFilename,
            creator.Name,
            note.CreatedAt
        );
    }
}
