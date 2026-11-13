using EnrollDesk.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace EnrollDesk.Api.Dtos;

/// <summary>Maps rules-engine outcomes to RFC 7807 problem responses with a consistent status-code policy.</summary>
public static class ServiceResultMapper
{
    public static ObjectResult ToProblem(EnrollmentOutcome outcome, string message) => outcome switch
    {
        EnrollmentOutcome.SectionNotFound => Problem(StatusCodes.Status404NotFound, message),
        EnrollmentOutcome.AlreadyEnrolled => Problem(StatusCodes.Status409Conflict, message),
        EnrollmentOutcome.ScheduleConflict => Problem(StatusCodes.Status409Conflict, message),
        EnrollmentOutcome.PrerequisiteNotMet => Problem(StatusCodes.Status422UnprocessableEntity, message),
        EnrollmentOutcome.CreditCapExceeded => Problem(StatusCodes.Status422UnprocessableEntity, message),
        EnrollmentOutcome.ConcurrencyConflict => Problem(StatusCodes.Status409Conflict, message),
        _ => Problem(StatusCodes.Status400BadRequest, message)
    };

    public static ObjectResult ToProblem(DropOutcome outcome, string message) => outcome switch
    {
        DropOutcome.NotFound => Problem(StatusCodes.Status404NotFound, message),
        DropOutcome.NotActive => Problem(StatusCodes.Status409Conflict, message),
        DropOutcome.NotAllowedPastDeadline => Problem(StatusCodes.Status422UnprocessableEntity, message),
        DropOutcome.ConcurrencyConflict => Problem(StatusCodes.Status409Conflict, message),
        _ => Problem(StatusCodes.Status400BadRequest, message)
    };

    public static ObjectResult ToProblem(GradeOutcome outcome, string message) => outcome switch
    {
        GradeOutcome.NotFound => Problem(StatusCodes.Status404NotFound, message),
        GradeOutcome.NotActive => Problem(StatusCodes.Status409Conflict, message),
        GradeOutcome.TermNotEnded => Problem(StatusCodes.Status422UnprocessableEntity, message),
        _ => Problem(StatusCodes.Status400BadRequest, message)
    };

    private static ObjectResult Problem(int statusCode, string detail) => new(new ProblemDetails
    {
        Status = statusCode,
        Title = ReasonPhrases.GetReasonPhrase(statusCode),
        Detail = detail
    })
    {
        StatusCode = statusCode
    };
}
