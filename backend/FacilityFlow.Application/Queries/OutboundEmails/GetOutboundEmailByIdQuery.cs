using FacilityFlow.Application.DTOs.OutboundEmails;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.OutboundEmails;

public record GetOutboundEmailByIdQuery(Guid Id) : IRequest<OutboundEmailDetailDto?>;

public class GetOutboundEmailByIdQueryHandler
    : IRequestHandler<GetOutboundEmailByIdQuery, OutboundEmailDetailDto?>
{
    private readonly IRepository<OutboundEmail> _emails;

    public GetOutboundEmailByIdQueryHandler(IRepository<OutboundEmail> emails) => _emails = emails;

    public async Task<OutboundEmailDetailDto?> Handle(
        GetOutboundEmailByIdQuery request, CancellationToken cancellationToken)
    {
        var email = await _emails.Query()
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (email is null)
            return null;

        return new OutboundEmailDetailDto(
            email.Id,
            email.ServiceRequestId,
            email.RecipientAddress,
            email.RecipientName,
            email.Subject,
            email.BodyHtml,
            email.SentAt,
            email.SentByName,
            email.EmailType.ToString(),
            email.ConversationId,
            email.Attachments.Select(a => new OutboundEmailAttachmentDto(
                a.Id, a.FileName, a.ContentType, a.FileSize
            )).ToList()
        );
    }
}
