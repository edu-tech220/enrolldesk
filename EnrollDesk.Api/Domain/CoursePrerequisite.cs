namespace EnrollDesk.Api.Domain;

/// <summary>Self-referencing link: <see cref="CourseId"/> requires <see cref="RequiredCourseId"/> to be completed first.</summary>
public class CoursePrerequisite
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int RequiredCourseId { get; set; }
    public Course RequiredCourse { get; set; } = null!;
}
