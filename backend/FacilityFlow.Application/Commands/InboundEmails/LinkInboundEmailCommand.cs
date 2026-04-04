using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.InboundEmails;

public record LinkInboundEmailCommand(Guid EmailId, Guid ServiceRequestId) : IRequest<bool>;

public class LinkInboundEmailCommandHandler : IRequestHandler<LinkInboundEmailCommand, bool>
{
    private readonly IRepository<InboundEmail> _emails;

    public LinkInboundEmailCommandHandler(IRepository<InboundEmail> emails) => _emails = emails;

    public async Task<bool> Handle(LinkInboundEmailCommand request, CancellationToken cancellationToken)
    {
        var email = await _emails.GetByIdAsync(request.EmailId);
        if (email is null)
            return false;

        email.ServiceRequestId = request.ServiceRequestId;
        await _emails.SaveChangesAsync();
        return true;
    }
}
