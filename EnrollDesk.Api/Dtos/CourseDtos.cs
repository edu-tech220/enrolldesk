namespace EnrollDesk.Api.Dtos;

public record CourseResponse(int Id, string Code, string Title, string Description, int Credits, IReadOnlyList<string> PrerequisiteCodes);

public record CreateCourseRequest(string Code, string Title, string Description, int Credits, List<string>? PrerequisiteCodes);
