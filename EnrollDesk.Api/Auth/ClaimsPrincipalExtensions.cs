using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EnrollDesk.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
