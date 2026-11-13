namespace EnrollDesk.Web.Models;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string FullName, string Email, string Password);

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
