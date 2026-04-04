using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.InboundEmails;

public record InboundEmailAttachmentResult(string FileName, string ContentType, string FilePath);

public record GetInboundEmailAttachmentQuery(Guid EmailId, Guid AttachmentId)
    : IRequest<InboundEmailAttachmentResult?>;

public class GetInboundEmailAttachmentQueryHandler
    : IRequestHandler<GetInboundEmailAttachmentQuery, InboundEmailAttachmentResult?>
{
    private readonly IRepository<InboundEmailAttachment> _attachments;

    public GetInboundEmailAttachmentQueryHandler(IRepository<InboundEmailAttachment> attachments)
        => _attachments = attachments;

    public async Task<InboundEmailAttachmentResult?> Handle(
        GetInboundEmailAttachmentQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(
                a => a.Id == request.AttachmentId && a.InboundEmailId == request.EmailId,
                cancellationToken);

        if (attachment is null)
            return null;

        return new InboundEmailAttachmentResult(
            attachment.FileName, attachment.ContentType, attachment.FilePath);
    }
}
