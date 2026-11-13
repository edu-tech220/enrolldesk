namespace EnrollDesk.Api.Dtos;

public record SectionResponse(
    int Id,
    int CourseId,
    string CourseCode,
    string CourseTitle,
    int TermId,
    string TermName,
    string SectionNumber,
    string InstructorName,
    string Location,
    List<string> Days,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int Capacity,
    int SeatsTaken,
    int SeatsAvailable,
    int WaitlistCount);

public record CreateSectionRequest(
    int CourseId,
    int TermId,
    string SectionNumber,
    string InstructorName,
    string Location,
    List<string> Days,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int Capacity);
