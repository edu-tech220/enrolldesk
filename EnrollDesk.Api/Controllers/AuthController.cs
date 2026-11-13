using EnrollDesk.Api.Auth;
using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Dtos;
using EnrollDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly EnrollDeskDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(EnrollDeskDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "An account with this email already exists.");
        }

        var (hash, salt) = PasswordHasher.Hash(request.Password);
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Student,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var (token, expiresAtUtc) = _tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAtUtc, user.FullName, user.Email, user.Role.ToString()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");
        }

        var (token, expiresAtUtc) = _tokenService.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAtUtc, user.FullName, user.Email, user.Role.ToString()));
    }
}
