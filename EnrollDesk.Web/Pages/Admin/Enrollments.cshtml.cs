using EnrollDesk.Web.Models;
using EnrollDesk.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnrollDesk.Web.Pages.Admin;

[Authorize(Roles = "Registrar")]
public class EnrollmentsModel : PageModel
{
    private readonly ApiClient _api;

    public EnrollmentsModel(ApiClient api)
    {
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public int? SectionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<EnrollmentDto> Result { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var result = await _api.GetAllEnrollmentsAsync(SectionId, Status, PageNumber);
        if (result.Success)
        {
            Result = result.Data!;
        }
        else
        {
            ErrorMessage = result.Error;
        }
    }

    public async Task<IActionResult> OnPostDropAsync(int enrollmentId)
    {
        var result = await _api.DropAsync(enrollmentId);
        StatusMessage = result.Success ? "Updated." : $"Could not drop: {result.Error}";
        return RedirectToPage(new { SectionId, Status, PageNumber });
    }

    public async Task<IActionResult> OnPostGradeAsync(int enrollmentId, decimal finalScore)
    {
        var result = await _api.GradeAsync(enrollmentId, finalScore);
        StatusMessage = result.Success ? "Grade recorded." : $"Could not record grade: {result.Error}";
        return RedirectToPage(new { SectionId, Status, PageNumber });
    }
}
