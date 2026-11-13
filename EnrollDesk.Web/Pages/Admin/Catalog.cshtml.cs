using System.ComponentModel.DataAnnotations;
using EnrollDesk.Web.Models;
using EnrollDesk.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnrollDesk.Web.Pages.Admin;

[Authorize(Roles = "Registrar")]
public class CatalogModel : PageModel
{
    private readonly ApiClient _api;

    public CatalogModel(ApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public NewCourseInput NewCourse { get; set; } = new();

    [BindProperty]
    public NewTermInput NewTerm { get; set; } = new();

    [BindProperty]
    public NewSectionInput NewSection { get; set; } = new();

    public List<CourseDto> Courses { get; private set; } = new();
    public List<TermDto> Terms { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadReferenceDataAsync();
    }

    public async Task<IActionResult> OnPostCreateCourseAsync()
    {
        var prerequisites = (NewCourse.PrerequisiteCodes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var result = await _api.CreateCourseAsync(new CreateCourseRequest(
            NewCourse.Code, NewCourse.Title, NewCourse.Description, NewCourse.Credits, prerequisites));

        StatusMessage = result.Success ? $"Course {result.Data!.Code} created." : $"Could not create course: {result.Error}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateTermAsync()
    {
        var result = await _api.CreateTermAsync(new CreateTermRequest(
            NewTerm.Name, NewTerm.StartDate, NewTerm.EndDate, NewTerm.AddDropDeadline, NewTerm.WithdrawalDeadline, NewTerm.MaxCreditsPerTerm, NewTerm.IsActive));

        StatusMessage = result.Success ? $"Term {result.Data!.Name} created." : $"Could not create term: {result.Error}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateSectionAsync()
    {
        var days = (NewSection.Days ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var result = await _api.CreateSectionAsync(new CreateSectionRequest(
            NewSection.CourseId, NewSection.TermId, NewSection.SectionNumber, NewSection.InstructorName,
            NewSection.Location, days, NewSection.StartTime, NewSection.EndTime, NewSection.Capacity));

        StatusMessage = result.Success
            ? $"Section {result.Data!.CourseCode}-{result.Data.SectionNumber} created."
            : $"Could not create section: {result.Error}";
        return RedirectToPage();
    }

    private async Task LoadReferenceDataAsync()
    {
        var courses = await _api.GetCoursesAsync(pageSize: 100);
        if (courses.Success)
        {
            Courses = courses.Data!.Items;
        }

        var terms = await _api.GetActiveTermAsync();
        if (terms.Success)
        {
            Terms = new List<TermDto> { terms.Data! };
        }
    }

    public class NewCourseInput
    {
        [Required] public string Code { get; set; } = string.Empty;
        [Required] public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Range(1, 10)] public int Credits { get; set; } = 3;
        public string? PrerequisiteCodes { get; set; }
    }

    public class NewTermInput
    {
        [Required] public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateOnly AddDropDeadline { get; set; }
        public DateOnly WithdrawalDeadline { get; set; }
        [Range(1, 30)] public int MaxCreditsPerTerm { get; set; } = 18;
        public bool IsActive { get; set; }
    }

    public class NewSectionInput
    {
        [Required] public int CourseId { get; set; }
        [Required] public int TermId { get; set; }
        [Required] public string SectionNumber { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        [Required] public string Days { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        [Range(1, 500)] public int Capacity { get; set; } = 30;
    }
}
