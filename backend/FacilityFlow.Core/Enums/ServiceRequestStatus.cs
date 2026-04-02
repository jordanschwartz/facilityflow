namespace FacilityFlow.Core.Enums;

public enum ServiceRequestStatus
{
    // Intake & Qualification
    New,
    Qualifying,

    // Sourcing
    Sourcing,

    // Scheduling / Assessment
    SchedulingSiteVisit,
    ScheduleConfirmed,

    // Quoting & Approval
    PendingQuotes,
    ProposalReady,
    PendingApproval,

    // PO Gate
    AwaitingPO,
    POReceived,

    // Execution
    JobInProgress,
    JobCompleted,

    // Verification
    Verification,

    // Billing
    InvoiceSent,
    InvoicePaid,

    // Final
    Closed,
    Cancelled
}
