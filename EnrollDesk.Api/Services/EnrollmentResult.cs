using EnrollDesk.Api.Domain;

namespace EnrollDesk.Api.Services;

public record EnrollmentResult(EnrollmentOutcome Outcome, string Message, Enrollment? Enrollment = null)
{
    public bool IsSuccess => Outcome is EnrollmentOutcome.Enrolled or EnrollmentOutcome.Waitlisted;
}
