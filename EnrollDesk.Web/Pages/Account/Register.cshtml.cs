using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnrollDesk.Web.Models;
using EnrollDesk.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnrollDesk.Web.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly ApiClient _api;

    public RegisterModel(ApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _api.RegisterAsync(new RegisterRequest(Input.FullName, Input.Email, Input.Password));
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        var auth = result.Data!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, auth.FullName),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role),
            new(ApiAuthHandler.AccessTokenClaimType, auth.Token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            ExpiresUtc = auth.ExpiresAtUtc,
            IsPersistent = true
        });

        return RedirectToPage("/Index");
    }

    public class InputModel
    {
        [Required, StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }
}
