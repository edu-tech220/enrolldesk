namespace EnrollDesk.Api.Services;

public record DropResult(DropOutcome Outcome, string Message)
{
    public bool IsSuccess => Outcome is DropOutcome.Dropped or DropOutcome.Withdrawn or DropOutcome.LeftWaitlist;
}
