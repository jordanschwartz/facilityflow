using System.Text.RegularExpressions;
using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.OutboundEmails;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.OutboundEmails;

public record GetOutboundEmailsByServiceRequestQuery(Guid ServiceRequestId, int Page, int PageSize)
    : IRequest<PagedResult<OutboundEmailDto>>;

public class GetOutboundEmailsByServiceRequestQueryHandler
    : IRequestHandler<GetOutboundEmailsByServiceRequestQuery, PagedResult<OutboundEmailDto>>
{
    private readonly IRepository<OutboundEmail> _emails;

    public GetOutboundEmailsByServiceRequestQueryHandler(IRepository<OutboundEmail> emails) => _emails = emails;

    public async Task<PagedResult<OutboundEmailDto>> Handle(
        GetOutboundEmailsByServiceRequestQuery request, CancellationToken cancellationToken)
    {
        var query = _emails.Query()
            .Include(e => e.Attachments)
            .Where(e => e.ServiceRequestId == request.ServiceRequestId)
            .OrderByDescending(e => e.SentAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var emails = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = emails.Select(e => new OutboundEmailDto(
            e.Id,
            e.ServiceRequestId,
            e.RecipientAddress,
            e.RecipientName,
            e.Subject,
            GeneratePreview(e.BodyHtml),
            e.SentAt,
            e.SentByName,
            e.EmailType.ToString(),
            e.Attachments.Count
        )).ToList();

        return new PagedResult<OutboundEmailDto>(items, totalCount, request.Page, request.PageSize);
    }

    public static string? GeneratePreview(string? bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(bodyHtml))
            return null;

        var text = Regex.Replace(bodyHtml, "<[^>]+>", " ").Trim();
        text = Regex.Replace(text, @"\s+", " ");

        if (string.IsNullOrWhiteSpace(text))
            return null;

        return text.Length <= 200 ? text : text[..200] + "...";
    }
}
