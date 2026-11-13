using EnrollDesk.Api.Domain;

namespace EnrollDesk.Api.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
