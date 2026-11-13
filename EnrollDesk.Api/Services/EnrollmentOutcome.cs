namespace EnrollDesk.Api.Services;

public enum EnrollmentOutcome
{
    Enrolled,
    Waitlisted,
    SectionNotFound,
    AlreadyEnrolled,
    PrerequisiteNotMet,
    ScheduleConflict,
    CreditCapExceeded,
    ConcurrencyConflict
}
