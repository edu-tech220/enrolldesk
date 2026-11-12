namespace EnrollDesk.Api.Domain;

public class Course
{
    public int Id { get; set; }

    /// <summary>Catalog code, e.g. "CS101".</summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }

    public List<Section> Sections { get; set; } = new();

    /// <summary>Courses that must be completed before this one can be taken.</summary>
    public List<CoursePrerequisite> Prerequisites { get; set; } = new();
}
