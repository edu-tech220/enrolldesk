using System.Net.Http.Headers;

namespace EnrollDesk.Web.Services;

/// <summary>Attaches the JWT stored in the signed-in user's cookie claims to outgoing API requests.</summary>
public class ApiAuthHandler : DelegatingHandler
{
    public const string AccessTokenClaimType = "access_token";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
