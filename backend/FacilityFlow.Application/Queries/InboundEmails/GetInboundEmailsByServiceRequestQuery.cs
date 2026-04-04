using FacilityFlow.Application.DTOs.Common;
using FacilityFlow.Application.DTOs.InboundEmails;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.InboundEmails;

public record GetInboundEmailsByServiceRequestQuery(Guid ServiceRequestId, int Page, int PageSize)
    : IRequest<PagedResult<InboundEmailDto>>;

public class GetInboundEmailsByServiceRequestQueryHandler
    : IRequestHandler<GetInboundEmailsByServiceRequestQuery, PagedResult<InboundEmailDto>>
{
    private readonly IRepository<InboundEmail> _emails;

    public GetInboundEmailsByServiceRequestQueryHandler(IRepository<InboundEmail> emails) => _emails = emails;

    public async Task<PagedResult<InboundEmailDto>> Handle(
        GetInboundEmailsByServiceRequestQuery request, CancellationToken cancellationToken)
    {
        var query = _emails.Query()
            .Include(e => e.Attachments)
            .Where(e => e.ServiceRequestId == request.ServiceRequestId)
            .OrderByDescending(e => e.ReceivedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var emails = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = emails.Select(e => new InboundEmailDto(
            e.Id,
            e.ServiceRequestId,
            e.FromAddress,
            e.FromName,
            e.Subject,
            GeneratePreview(e.BodyText, e.BodyHtml),
            e.ReceivedAt,
            e.Attachments.Count
        )).ToList();

        return new PagedResult<InboundEmailDto>(items, totalCount, request.Page, request.PageSize);
    }

    private static string? GeneratePreview(string? bodyText, string? bodyHtml)
    {
        var text = bodyText;
        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(bodyHtml))
            text = System.Text.RegularExpressions.Regex.Replace(bodyHtml, "<[^>]+>", " ").Trim();

        if (string.IsNullOrWhiteSpace(text))
            return null;

        return text.Length <= 200 ? text : text[..200] + "...";
    }
}
