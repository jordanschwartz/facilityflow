using System.Security.Claims;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace FacilityFlow.Infrastructure.Services;

public class ActivityLogger : IActivityLogger
{
    private readonly IRepository<ActivityLog> _repo;
    private readonly IRepository<User> _users;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogger(IRepository<ActivityLog> repo, IRepository<User> users, IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _users = users;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(Guid serviceRequestId, Guid? workOrderId, string action, ActivityLogCategory category, string actorName, Guid? actorId)
    {
        // Auto-resolve actor from HTTP context or provided actorId
        if (string.IsNullOrEmpty(actorName))
        {
            if (actorId == null)
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
                    actorId = userId;
            }

            if (actorId != null)
            {
                var user = await _users.GetByIdAsync(actorId.Value);
                actorName = user?.Name ?? "Unknown";
            }
            else
            {
                actorName = "System";
            }
        }

        var entry = new ActivityLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequestId,
            WorkOrderId = workOrderId,
            Action = action,
            Category = category,
            ActorName = actorName,
            ActorId = actorId,
            CreatedAt = DateTime.UtcNow
        };

        _repo.Add(entry);
        await _repo.SaveChangesAsync();
    }
}
