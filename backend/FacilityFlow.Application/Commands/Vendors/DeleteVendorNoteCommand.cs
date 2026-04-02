using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Vendors;

public record DeleteVendorNoteCommand(Guid VendorId, Guid NoteId, Guid UserId, string UserRole) : IRequest<Unit>;

public class DeleteVendorNoteCommandHandler : IRequestHandler<DeleteVendorNoteCommand, Unit>
{
    private readonly IRepository<VendorNote> _noteRepo;

    public DeleteVendorNoteCommandHandler(IRepository<VendorNote> noteRepo) => _noteRepo = noteRepo;

    public async Task<Unit> Handle(DeleteVendorNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteRepo.Query()
            .FirstOrDefaultAsync(n => n.Id == request.NoteId && n.VendorId == request.VendorId, cancellationToken)
            ?? throw new NotFoundException("Note not found.");

        if (note.CreatedById != request.UserId && request.UserRole != "Operator")
            throw new ForbiddenException("You do not have permission to delete this note.");

        _noteRepo.Remove(note);
        await _noteRepo.SaveChangesAsync();
        return Unit.Value;
    }
}
