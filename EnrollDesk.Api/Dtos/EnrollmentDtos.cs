namespace EnrollDesk.Api.Dtos;

public record CreateEnrollmentRequest(int SectionId, bool OverrideCreditCap = false);

public record GradeRequest(decimal FinalScore);

public record EnrollmentResponse(
    int Id,
    int UserId,
    string StudentName,
    int SectionId,
    string CourseCode,
    string SectionNumber,
    string TermName,
    string Status,
    int? WaitlistPosition,
    decimal? FinalScore,
    DateTime CreatedAtUtc,
    DateTime? EndedAtUtc);
