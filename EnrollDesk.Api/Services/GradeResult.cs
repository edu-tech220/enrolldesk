namespace EnrollDesk.Api.Services;

public record GradeResult(GradeOutcome Outcome, string Message)
{
    public bool IsSuccess => Outcome == GradeOutcome.Recorded;
}
