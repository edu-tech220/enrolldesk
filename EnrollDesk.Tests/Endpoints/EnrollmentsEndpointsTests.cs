using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Dtos;
using EnrollDesk.Tests.TestSupport;
using Xunit;

namespace EnrollDesk.Tests.Endpoints;

public class EnrollmentsEndpointsTests : IAsyncLifetime
{
    private readonly EnrollDeskApiFactory _factory = new();
    private HttpClient _client = null!;
    private int _sectionId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();

        _sectionId = await _factory.ExecuteDbAsync(async db =>
        {
            var term = new Term
            {
                Name = "2099-1", StartDate = new DateOnly(2099, 1, 1), EndDate = new DateOnly(2099, 5, 1),
                AddDropDeadline = new DateOnly(2099, 1, 20), WithdrawalDeadline = new DateOnly(2099, 3, 1),
                MaxCreditsPerTerm = 18, IsActive = true
            };
            var course = new Course { Code = "CS101", Title = "Intro to Programming", Credits = 4 };
            db.AddRange(term, course);
            await db.SaveChangesAsync();

            var section = new Section
            {
                CourseId = course.Id, TermId = term.Id, SectionNumber = "001",
                Days = MeetingDays.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
                Capacity = 10
            };
            db.Sections.Add(section);
            await db.SaveChangesAsync();
            return section.Id;
        });

        _factory.Clock.UtcNow = new DateTime(2099, 1, 10, 0, 0, 0, DateTimeKind.Utc);
    }

    public Task DisposeAsync() => _factory.DisposeAsync();

    private async Task<string> RegisterStudentAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Jamie Torres", email, "Secret#123"));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    private static HttpRequestMessage WithBearer(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Enroll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/enrollments", new CreateEnrollmentRequest(_sectionId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enroll_AsAuthenticatedStudent_ReturnsCreated()
    {
        var token = await RegisterStudentAsync("enroll1@test.local");

        var response = await _client.SendAsync(WithBearer(HttpMethod.Post, "/api/enrollments", token, new CreateEnrollmentRequest(_sectionId)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrollment = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();
        Assert.Equal("Enrolled", enrollment!.Status);
    }

    [Fact]
    public async Task Drop_ByOwner_Succeeds()
    {
        var token = await RegisterStudentAsync("enroll2@test.local");
        var enrollResponse = await _client.SendAsync(WithBearer(HttpMethod.Post, "/api/enrollments", token, new CreateEnrollmentRequest(_sectionId)));
        var enrollment = await enrollResponse.Content.ReadFromJsonAsync<EnrollmentResponse>();

        var response = await _client.SendAsync(WithBearer(HttpMethod.Post, $"/api/enrollments/{enrollment!.Id}/drop", token));

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Drop_OfSomeoneElsesEnrollment_ReturnsNotFound()
    {
        var ownerToken = await RegisterStudentAsync("owner@test.local");
        var enrollResponse = await _client.SendAsync(WithBearer(HttpMethod.Post, "/api/enrollments", ownerToken, new CreateEnrollmentRequest(_sectionId)));
        var enrollment = await enrollResponse.Content.ReadFromJsonAsync<EnrollmentResponse>();

        var otherToken = await RegisterStudentAsync("other@test.local");
        var response = await _client.SendAsync(WithBearer(HttpMethod.Post, $"/api/enrollments/{enrollment!.Id}/drop", otherToken));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsStudent_ReturnsForbidden()
    {
        var token = await RegisterStudentAsync("list@test.local");

        var response = await _client.SendAsync(WithBearer(HttpMethod.Get, "/api/enrollments", token));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Grade_AsRegistrarAfterTermEnds_Succeeds()
    {
        var studentToken = await RegisterStudentAsync("grade1@test.local");
        var enrollResponse = await _client.SendAsync(WithBearer(HttpMethod.Post, "/api/enrollments", studentToken, new CreateEnrollmentRequest(_sectionId)));
        var enrollment = await enrollResponse.Content.ReadFromJsonAsync<EnrollmentResponse>();

        _factory.Clock.UtcNow = new DateTime(2099, 5, 2, 0, 0, 0, DateTimeKind.Utc);
        var registrarToken = await _factory.CreateRegistrarTokenAsync();

        var response = await _client.SendAsync(WithBearer(HttpMethod.Post, $"/api/enrollments/{enrollment!.Id}/grade", registrarToken, new GradeRequest(88m)));

        response.EnsureSuccessStatusCode();
    }
}
