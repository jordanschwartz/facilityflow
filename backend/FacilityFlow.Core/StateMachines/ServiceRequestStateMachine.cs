using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.StateMachines;

public static class ServiceRequestStateMachine
{
    private static readonly Dictionary<ServiceRequestStatus, ServiceRequestStatus[]> _allowed = new()
    {
        [ServiceRequestStatus.New]             = [ServiceRequestStatus.Sourcing, ServiceRequestStatus.Rejected],
        [ServiceRequestStatus.Sourcing]        = [ServiceRequestStatus.Quoting, ServiceRequestStatus.Rejected],
        [ServiceRequestStatus.Quoting]         = [ServiceRequestStatus.PendingApproval, ServiceRequestStatus.Sourcing, ServiceRequestStatus.Rejected],
        [ServiceRequestStatus.PendingApproval] = [ServiceRequestStatus.Approved, ServiceRequestStatus.Rejected],
        [ServiceRequestStatus.Approved]        = [ServiceRequestStatus.Completed],
        [ServiceRequestStatus.Rejected]        = [ServiceRequestStatus.Sourcing],
        [ServiceRequestStatus.Completed]       = [],
    };

    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to)
        => _allowed.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
