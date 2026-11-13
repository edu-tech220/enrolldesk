using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnrollDesk.Api.Dtos;
using EnrollDesk.Tests.TestSupport;
using Xunit;

namespace EnrollDesk.Tests.Endpoints;

public class CoursesEndpointsTests : IAsyncLifetime
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
    public async Task GetAll_WithoutAuthentication_ReturnsEmptyPageInitially()
    {
        var response = await _client.GetAsync("/api/courses");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<CourseResponse>>();
        Assert.Equal(0, page!.TotalCount);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/courses", new CreateCourseRequest("CS101", "Intro", "desc", 4, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsStudent_ReturnsForbidden()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Jamie", "student1@test.local", "Secret#123"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/courses")
        {
            Content = JsonContent.Create(new CreateCourseRequest("CS101", "Intro", "desc", 4, null))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsRegistrar_Succeeds()
    {
        var token = await _factory.CreateRegistrarTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/courses")
        {
            Content = JsonContent.Create(new CreateCourseRequest("CS101", "Intro to Programming", "desc", 4, null))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var course = await response.Content.ReadFromJsonAsync<CourseResponse>();
        Assert.Equal("CS101", course!.Code);
    }

    [Fact]
    public async Task Create_WithUnknownPrerequisite_ReturnsBadRequest()
    {
        var token = await _factory.CreateRegistrarTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/courses")
        {
            Content = JsonContent.Create(new CreateCourseRequest("CS201", "Data Structures", "desc", 4, new List<string> { "CS999" }))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
