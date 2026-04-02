using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.StateMachines;

public static class ServiceRequestStateMachine
{
    private static readonly Dictionary<ServiceRequestStatus, ServiceRequestStatus[]> _allowed = new()
    {
        [ServiceRequestStatus.New]                = [ServiceRequestStatus.Qualifying, ServiceRequestStatus.Sourcing, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Qualifying]         = [ServiceRequestStatus.Sourcing, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.Sourcing]           = [ServiceRequestStatus.SchedulingSiteVisit, ServiceRequestStatus.PendingQuotes, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.SchedulingSiteVisit]= [ServiceRequestStatus.ScheduleConfirmed, ServiceRequestStatus.Sourcing, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.ScheduleConfirmed]  = [ServiceRequestStatus.PendingQuotes, ServiceRequestStatus.Sourcing, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.PendingQuotes]      = [ServiceRequestStatus.ProposalReady, ServiceRequestStatus.Sourcing, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.ProposalReady]      = [ServiceRequestStatus.PendingApproval, ServiceRequestStatus.PendingQuotes, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.PendingApproval]    = [ServiceRequestStatus.AwaitingPO, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.AwaitingPO]         = [ServiceRequestStatus.POReceived, ServiceRequestStatus.Cancelled],
        [ServiceRequestStatus.POReceived]         = [ServiceRequestStatus.JobInProgress],
        [ServiceRequestStatus.JobInProgress]      = [ServiceRequestStatus.JobCompleted],
        [ServiceRequestStatus.JobCompleted]       = [ServiceRequestStatus.Verification, ServiceRequestStatus.InvoiceSent],
        [ServiceRequestStatus.Verification]       = [ServiceRequestStatus.InvoiceSent, ServiceRequestStatus.JobInProgress],
        [ServiceRequestStatus.InvoiceSent]        = [ServiceRequestStatus.InvoicePaid],
        [ServiceRequestStatus.InvoicePaid]        = [ServiceRequestStatus.Closed],
        [ServiceRequestStatus.Closed]             = [ServiceRequestStatus.New],  // reopen
        [ServiceRequestStatus.Cancelled]          = [ServiceRequestStatus.New],  // reopen
    };

    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to)
        => _allowed.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static ServiceRequestStatus[] GetAllowedTransitions(ServiceRequestStatus from)
        => _allowed.TryGetValue(from, out var allowed) ? allowed : [];
}
