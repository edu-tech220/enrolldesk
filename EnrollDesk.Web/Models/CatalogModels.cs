namespace EnrollDesk.Web.Models;

public class CourseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public List<string> PrerequisiteCodes { get; set; } = new();
}

public record CreateCourseRequest(string Code, string Title, string Description, int Credits, List<string>? PrerequisiteCodes);

public class TermDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly AddDropDeadline { get; set; }
    public DateOnly WithdrawalDeadline { get; set; }
    public int MaxCreditsPerTerm { get; set; }
    public bool IsActive { get; set; }
}

public record CreateTermRequest(
    string Name, DateOnly StartDate, DateOnly EndDate, DateOnly AddDropDeadline, DateOnly WithdrawalDeadline, int MaxCreditsPerTerm, bool IsActive);

public class SectionDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public int TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string SectionNumber { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Days { get; set; } = new();
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Capacity { get; set; }
    public int SeatsTaken { get; set; }
    public int SeatsAvailable { get; set; }
    public int WaitlistCount { get; set; }
}

public record CreateSectionRequest(
    int CourseId, int TermId, string SectionNumber, string InstructorName, string Location,
    List<string> Days, TimeSpan StartTime, TimeSpan EndTime, int Capacity);
