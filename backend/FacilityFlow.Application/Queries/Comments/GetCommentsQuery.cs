using FacilityFlow.Application.DTOs.Comments;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Interfaces.Repositories;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Comments;

public record GetCommentsQuery(Guid? ServiceRequestId, Guid? QuoteId, Guid? WorkOrderId) : IRequest<List<CommentDto>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, List<CommentDto>>
{
    private readonly IRepository<Comment> _comments;

    public GetCommentsQueryHandler(IRepository<Comment> comments) => _comments = comments;

    public async Task<List<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var query = _comments.Query().Include(c => c.Author).AsQueryable();

        if (request.ServiceRequestId.HasValue)
            query = query.Where(c => c.ServiceRequestId == request.ServiceRequestId.Value);
        else if (request.QuoteId.HasValue)
            query = query.Where(c => c.QuoteId == request.QuoteId.Value);
        else if (request.WorkOrderId.HasValue)
            query = query.Where(c => c.WorkOrderId == request.WorkOrderId.Value);

        var comments = await query.OrderBy(c => c.CreatedAt).ToListAsync(cancellationToken);

        return comments.Select(c => new CommentDto(
            c.Id,
            c.Text,
            c.AuthorId,
            c.ServiceRequestId,
            c.QuoteId,
            c.WorkOrderId,
            c.CreatedAt,
            c.Author.Adapt<UserDto>()
        )).ToList();
    }
}
