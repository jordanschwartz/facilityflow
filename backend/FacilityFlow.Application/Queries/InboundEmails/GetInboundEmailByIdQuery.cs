using FacilityFlow.Application.DTOs.InboundEmails;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.InboundEmails;

public record GetInboundEmailByIdQuery(Guid Id) : IRequest<InboundEmailDetailDto?>;

public class GetInboundEmailByIdQueryHandler
    : IRequestHandler<GetInboundEmailByIdQuery, InboundEmailDetailDto?>
{
    private readonly IRepository<InboundEmail> _emails;

    public GetInboundEmailByIdQueryHandler(IRepository<InboundEmail> emails) => _emails = emails;

    public async Task<InboundEmailDetailDto?> Handle(
        GetInboundEmailByIdQuery request, CancellationToken cancellationToken)
    {
        var email = await _emails.Query()
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (email is null)
            return null;

        return new InboundEmailDetailDto(
            email.Id,
            email.ServiceRequestId,
            email.FromAddress,
            email.FromName,
            email.Subject,
            email.BodyText,
            email.BodyHtml,
            email.ReceivedAt,
            email.MessageId,
            email.Attachments.Select(a => new InboundEmailAttachmentDto(
                a.Id, a.FileName, a.ContentType, a.FileSize
            )).ToList()
        );
    }
}
