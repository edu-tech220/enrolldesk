using EnrollDesk.Web.Models;
using EnrollDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnrollDesk.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ApiClient _api;

    public IndexModel(ApiClient api)
    {
        _api = api;
    }

    public TermDto? ActiveTerm { get; private set; }
    public List<SectionDto> Sections { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var termResult = await _api.GetActiveTermAsync();
        if (!termResult.Success)
        {
            ErrorMessage = termResult.Error;
            return;
        }

        ActiveTerm = termResult.Data;
        var sectionsResult = await _api.GetSectionsAsync(ActiveTerm!.Id, pageSize: 100);
        if (sectionsResult.Success)
        {
            Sections = sectionsResult.Data!.Items;
        }
        else
        {
            ErrorMessage = sectionsResult.Error;
        }
    }

    public async Task<IActionResult> OnPostEnrollAsync(int sectionId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = "/" });
        }

        var result = await _api.EnrollAsync(sectionId);
        StatusMessage = result.Success
            ? $"{result.Data!.Status}: {(result.Data.Status == "Waitlisted" ? $"position {result.Data.WaitlistPosition}" : "you're in!")}"
            : $"Could not enroll: {result.Error}";

        return RedirectToPage();
    }
}
