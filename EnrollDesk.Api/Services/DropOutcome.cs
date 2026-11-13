namespace EnrollDesk.Api.Services;

public enum DropOutcome
{
    Dropped,
    Withdrawn,
    LeftWaitlist,
    NotFound,
    NotActive,
    NotAllowedPastDeadline,
    ConcurrencyConflict
}
