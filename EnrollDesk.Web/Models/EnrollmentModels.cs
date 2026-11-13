namespace EnrollDesk.Web.Models;

public record CreateEnrollmentRequest(int SectionId, bool OverrideCreditCap = false);

public record GradeRequest(decimal FinalScore);

public class EnrollmentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int SectionId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string SectionNumber { get; set; } = string.Empty;
    public string TermName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? WaitlistPosition { get; set; }
    public decimal? FinalScore { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
}
