using EnrollDesk.Web.Models;
using EnrollDesk.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnrollDesk.Web.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly ApiClient _api;

    public DashboardModel(ApiClient api)
    {
        _api = api;
    }

    public List<EnrollmentDto> Enrollments { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var result = await _api.GetMyEnrollmentsAsync();
        if (result.Success)
        {
            Enrollments = result.Data!;
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
        return RedirectToPage();
    }
}
