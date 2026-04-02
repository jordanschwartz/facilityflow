using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Quotes;

public record UploadQuoteAttachmentCommand(string Token, Stream FileStream, string FileName, string ContentType) : IRequest<AttachmentDto>;

public class UploadQuoteAttachmentCommandHandler : IRequestHandler<UploadQuoteAttachmentCommand, AttachmentDto>
{
    private readonly IQuoteRepository _quotes;
    private readonly IRepository<Attachment> _attachments;
    private readonly IFileStorageService _fileStorage;

    public UploadQuoteAttachmentCommandHandler(IQuoteRepository quotes, IRepository<Attachment> attachments, IFileStorageService fileStorage)
    {
        _quotes = quotes;
        _attachments = attachments;
        _fileStorage = fileStorage;
    }

    public async Task<AttachmentDto> Handle(UploadQuoteAttachmentCommand command, CancellationToken cancellationToken)
    {
        var quote = await _quotes.Query()
            .FirstOrDefaultAsync(q => q.PublicToken == command.Token, cancellationToken)
            ?? throw new NotFoundException("Quote not found.");

        if (quote.Status != QuoteStatus.Requested)
            throw new InvalidOperationException("Quote is no longer accepting attachments.");

        if (!_fileStorage.AllowedMimeTypes.Contains(command.ContentType))
            throw new InvalidOperationException("File type not allowed. Accepted: images, videos, PDF.");

        var (url, savedFilename) = await _fileStorage.SaveFileAsync(
            quote.Id.ToString(), command.FileStream, command.FileName, command.ContentType);

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            QuoteId = quote.Id,
            Filename = command.FileName,
            MimeType = command.ContentType,
            Url = url
        };

        _attachments.Add(attachment);
        await _attachments.SaveChangesAsync();

        return new AttachmentDto(attachment.Id, attachment.Url, attachment.Filename, attachment.MimeType);
    }
}
