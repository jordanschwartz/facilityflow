using System.Text.RegularExpressions;
using FacilityFlow.Application.DTOs.EmailConversations;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Helpers;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.EmailConversations;

public record GetEmailConversationsQuery(Guid ServiceRequestId)
    : IRequest<List<EmailConversationDto>>;

public class GetEmailConversationsQueryHandler
    : IRequestHandler<GetEmailConversationsQuery, List<EmailConversationDto>>
{
    private readonly IRepository<InboundEmail> _inboundEmails;
    private readonly IRepository<OutboundEmail> _outboundEmails;

    public GetEmailConversationsQueryHandler(
        IRepository<InboundEmail> inboundEmails,
        IRepository<OutboundEmail> outboundEmails)
    {
        _inboundEmails = inboundEmails;
        _outboundEmails = outboundEmails;
    }

    public async Task<List<EmailConversationDto>> Handle(
        GetEmailConversationsQuery request, CancellationToken cancellationToken)
    {
        var inboundEmails = await _inboundEmails.Query()
            .Include(e => e.Attachments)
            .Where(e => e.ServiceRequestId == request.ServiceRequestId && e.ConversationId != null)
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync(cancellationToken);

        var outboundEmails = await _outboundEmails.Query()
            .Include(e => e.Attachments)
            .Where(e => e.ServiceRequestId == request.ServiceRequestId && e.ConversationId != null)
            .OrderBy(e => e.SentAt)
            .ToListAsync(cancellationToken);

        var inboundItems = inboundEmails.Select(e => new
        {
            ConvId = e.ConversationId!,
            Item = new EmailThreadItemDto(
                e.Id,
                "inbound",
                e.FromAddress,
                e.FromName,
                null,
                null,
                e.Subject,
                GeneratePreview(e.BodyText, e.BodyHtml),
                e.ReceivedAt,
                e.Attachments.Count)
        });

        var outboundItems = outboundEmails.Select(e => new
        {
            ConvId = e.ConversationId!,
            Item = new EmailThreadItemDto(
                e.Id,
                "outbound",
                e.SentByName,
                e.SentByName,
                e.RecipientAddress,
                e.RecipientName,
                e.Subject,
                GeneratePreview(null, e.BodyHtml),
                e.SentAt,
                e.Attachments.Count)
        });

        var allItems = inboundItems.Concat(outboundItems);

        var conversations = allItems
            .GroupBy(x => x.ConvId)
            .Select(g =>
            {
                var emails = g.Select(x => x.Item).OrderBy(e => e.Timestamp).ToList();
                var firstSubject = emails.First().Subject;
                var normalizedSubject = ConversationResolver.NormalizeSubject(firstSubject);

                return new EmailConversationDto(
                    g.Key,
                    normalizedSubject,
                    emails.Max(e => e.Timestamp),
                    emails.Count,
                    emails);
            })
            .OrderByDescending(c => c.LatestEmailAt)
            .ToList();

        return conversations;
    }

    private static string? GeneratePreview(string? bodyText, string? bodyHtml)
    {
        var text = bodyText;
        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(bodyHtml))
            text = Regex.Replace(bodyHtml, "<[^>]+>", " ").Trim();

        if (string.IsNullOrWhiteSpace(text))
            return null;

        return text.Length <= 200 ? text : text[..200] + "...";
    }
}
