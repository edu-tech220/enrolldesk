using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnrollDesk.Web.Models;
using EnrollDesk.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnrollDesk.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly ApiClient _api;

    public LoginModel(ApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        Input.ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _api.LoginAsync(new LoginRequest(Input.Email, Input.Password));
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        await SignInAsync(result.Data!);
        return LocalRedirect(string.IsNullOrEmpty(Input.ReturnUrl) ? "/" : Input.ReturnUrl);
    }

    private async Task SignInAsync(AuthResponse auth)
    {
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
    }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
