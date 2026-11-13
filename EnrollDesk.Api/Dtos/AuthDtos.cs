namespace EnrollDesk.Api.Dtos;

public record RegisterRequest(string FullName, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, DateTime ExpiresAtUtc, string FullName, string Email, string Role);
