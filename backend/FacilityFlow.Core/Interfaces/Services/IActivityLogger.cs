using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Interfaces.Services;

public interface IActivityLogger
{
    Task LogAsync(Guid serviceRequestId, Guid? workOrderId, string action, ActivityLogCategory category, string actorName, Guid? actorId);
}
