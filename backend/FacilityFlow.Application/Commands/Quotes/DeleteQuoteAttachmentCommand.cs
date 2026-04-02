using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Quotes;

public record DeleteQuoteAttachmentCommand(string Token, Guid AttachmentId) : IRequest<Unit>;

public class DeleteQuoteAttachmentCommandHandler : IRequestHandler<DeleteQuoteAttachmentCommand, Unit>
{
    private readonly IQuoteRepository _quotes;
    private readonly IRepository<Attachment> _attachments;
    private readonly IFileStorageService _fileStorage;

    public DeleteQuoteAttachmentCommandHandler(IQuoteRepository quotes, IRepository<Attachment> attachments, IFileStorageService fileStorage)
    {
        _quotes = quotes;
        _attachments = attachments;
        _fileStorage = fileStorage;
    }

    public async Task<Unit> Handle(DeleteQuoteAttachmentCommand command, CancellationToken cancellationToken)
    {
        var quote = await _quotes.Query()
            .FirstOrDefaultAsync(q => q.PublicToken == command.Token, cancellationToken)
            ?? throw new NotFoundException("Quote not found.");

        if (quote.Status != QuoteStatus.Requested)
            throw new InvalidOperationException("Quote is no longer accepting changes.");

        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(a => a.Id == command.AttachmentId && a.QuoteId == quote.Id, cancellationToken)
            ?? throw new NotFoundException("Attachment not found.");

        _fileStorage.DeleteFile(attachment.Url);
        _attachments.Remove(attachment);
        await _attachments.SaveChangesAsync();

        return Unit.Value;
    }
}
