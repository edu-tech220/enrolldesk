using System.Net;
using System.Net.Http.Json;
using EnrollDesk.Api.Dtos;
using EnrollDesk.Tests.TestSupport;
using Xunit;

namespace EnrollDesk.Tests.Endpoints;

public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly EnrollDeskApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Register_WithNewEmail_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Jamie Torres", "jamie@test.local", "Secret#123"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
        Assert.Equal("Student", body.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var request = new RegisterRequest("Jamie Torres", "dup@test.local", "Secret#123");
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Jamie Torres", "wrongpass@test.local", "Secret#123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("wrongpass@test.local", "WrongPassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Jamie Torres", "correct@test.local", "Secret#123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("correct@test.local", "Secret#123"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
    }
}
