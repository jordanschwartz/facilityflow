using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.OutboundEmails;

public record OutboundEmailAttachmentResult(string FileName, string ContentType, string FilePath);

public record GetOutboundEmailAttachmentQuery(Guid EmailId, Guid AttachmentId)
    : IRequest<OutboundEmailAttachmentResult?>;

public class GetOutboundEmailAttachmentQueryHandler
    : IRequestHandler<GetOutboundEmailAttachmentQuery, OutboundEmailAttachmentResult?>
{
    private readonly IRepository<OutboundEmailAttachment> _attachments;

    public GetOutboundEmailAttachmentQueryHandler(IRepository<OutboundEmailAttachment> attachments)
        => _attachments = attachments;

    public async Task<OutboundEmailAttachmentResult?> Handle(
        GetOutboundEmailAttachmentQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(
                a => a.Id == request.AttachmentId && a.OutboundEmailId == request.EmailId,
                cancellationToken);

        if (attachment is null)
            return null;

        return new OutboundEmailAttachmentResult(
            attachment.FileName, attachment.ContentType, attachment.FilePath);
    }
}
