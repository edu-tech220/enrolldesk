using System.Net.Http.Json;
using System.Text.Json;
using EnrollDesk.Web.Models;

namespace EnrollDesk.Web.Services;

/// <summary>Thin wrapper over the EnrollDesk API, called through a typed <see cref="HttpClient"/> from Razor Pages.</summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<ApiResult<TermDto>> GetActiveTermAsync() => GetAsync<TermDto>("api/terms/active");

    public Task<ApiResult<PagedResult<CourseDto>>> GetCoursesAsync(int page = 1, int pageSize = 50) =>
        GetAsync<PagedResult<CourseDto>>($"api/courses?page={page}&pageSize={pageSize}");

    public Task<ApiResult<PagedResult<SectionDto>>> GetSectionsAsync(int? termId, int page = 1, int pageSize = 50)
    {
        var query = termId is null ? $"api/sections?page={page}&pageSize={pageSize}" : $"api/sections?termId={termId}&page={page}&pageSize={pageSize}";
        return GetAsync<PagedResult<SectionDto>>(query);
    }

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) => PostAsync<AuthResponse>("api/auth/login", request);

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request) => PostAsync<AuthResponse>("api/auth/register", request);

    public Task<ApiResult<EnrollmentDto>> EnrollAsync(int sectionId) =>
        PostAsync<EnrollmentDto>("api/enrollments", new CreateEnrollmentRequest(sectionId));

    public Task<ApiResult<List<EnrollmentDto>>> GetMyEnrollmentsAsync() => GetAsync<List<EnrollmentDto>>("api/enrollments/mine");

    public Task<ApiResult<PagedResult<EnrollmentDto>>> GetAllEnrollmentsAsync(int? sectionId, string? status, int page = 1, int pageSize = 20)
    {
        var query = $"api/enrollments?page={page}&pageSize={pageSize}";
        if (sectionId is not null) query += $"&sectionId={sectionId}";
        if (!string.IsNullOrWhiteSpace(status)) query += $"&status={status}";
        return GetAsync<PagedResult<EnrollmentDto>>(query);
    }

    public Task<ApiResult<object>> DropAsync(int enrollmentId) => PostAsync<object>($"api/enrollments/{enrollmentId}/drop", new { });

    public Task<ApiResult<object>> GradeAsync(int enrollmentId, decimal finalScore) =>
        PostAsync<object>($"api/enrollments/{enrollmentId}/grade", new GradeRequest(finalScore));

    public Task<ApiResult<CourseDto>> CreateCourseAsync(CreateCourseRequest request) => PostAsync<CourseDto>("api/courses", request);

    public Task<ApiResult<TermDto>> CreateTermAsync(CreateTermRequest request) => PostAsync<TermDto>("api/terms", request);

    public Task<ApiResult<SectionDto>> CreateSectionAsync(CreateSectionRequest request) => PostAsync<SectionDto>("api/sections", request);

    private async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        return await ToResultAsync<T>(response);
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object body)
    {
        var response = await _http.PostAsJsonAsync(url, body);
        return await ToResultAsync<T>(response);
    }

    private static async Task<ApiResult<T>> ToResultAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(object))
            {
                return ApiResult<T>.Ok(default!);
            }

            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return ApiResult<T>.Ok(data!);
        }

        var problem = await TryReadProblemAsync(response);
        return ApiResult<T>.Fail(problem ?? $"Request failed with status {(int)response.StatusCode}.");
    }

    private static async Task<string?> TryReadProblemAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(JsonOptions);
            return problem?.Detail ?? problem?.Title;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
