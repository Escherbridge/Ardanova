namespace ArdaNova.API.Client;

using System.Net.Http.Json;
using System.Text.Json;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

/// <summary>
/// TRPC-style API client for ArdaNova API
/// </summary>
public class ArdaNovaApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ArdaNovaApiClient(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public UserClient Users => new(_httpClient, _jsonOptions);
    public ProjectClient Projects => new(_httpClient, _jsonOptions);
    public AgencyClient Agencies => new(_httpClient, _jsonOptions);
    public BusinessClient Businesses => new(_httpClient, _jsonOptions);

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public class UserClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public UserClient(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id) =>
        await _http.GetFromJsonAsync<UserDto>($"api/users/{id}", _json);

    public async Task<IReadOnlyList<UserDto>?> GetAllAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<UserDto>>("api/users", _json);

    public async Task<PagedResult<UserDto>?> GetPagedAsync(int page = 1, int pageSize = 10) =>
        await _http.GetFromJsonAsync<PagedResult<UserDto>>($"api/users/paged?page={page}&pageSize={pageSize}", _json);

    public async Task<UserDto?> GetByEmailAsync(string email) =>
        await _http.GetFromJsonAsync<UserDto>($"api/users/email/{email}", _json);

    public async Task<UserDto?> CreateAsync(CreateUserDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/users", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(_json);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/users/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(_json);
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/users/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserDto?> VerifyAsync(Guid id)
    {
        var response = await _http.PostAsync($"api/users/{id}/verify", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(_json);
    }
}

public class ProjectClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public ProjectClient(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id) =>
        await _http.GetFromJsonAsync<ProjectDto>($"api/projects/{id}", _json);

    public async Task<IReadOnlyList<ProjectDto>?> GetAllAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<ProjectDto>>("api/projects", _json);

    public async Task<PagedResult<ProjectDto>?> GetPagedAsync(int page = 1, int pageSize = 10) =>
        await _http.GetFromJsonAsync<PagedResult<ProjectDto>>($"api/projects/paged?page={page}&pageSize={pageSize}", _json);

    public async Task<ProjectDto?> GetBySlugAsync(string slug) =>
        await _http.GetFromJsonAsync<ProjectDto>($"api/projects/slug/{slug}", _json);

    public async Task<IReadOnlyList<ProjectDto>?> GetFeaturedAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<ProjectDto>>("api/projects/featured", _json);

    public async Task<ProjectDto?> CreateAsync(CreateProjectDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/projects", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectDto>(_json);
    }

    public async Task<ProjectDto?> UpdateAsync(Guid id, UpdateProjectDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/projects/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectDto>(_json);
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/projects/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ProjectDto?> PublishAsync(Guid id)
    {
        var response = await _http.PostAsync($"api/projects/{id}/publish", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectDto>(_json);
    }
}

public class AgencyClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public AgencyClient(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<AgencyDto?> GetByIdAsync(Guid id) =>
        await _http.GetFromJsonAsync<AgencyDto>($"api/agencies/{id}", _json);

    public async Task<IReadOnlyList<AgencyDto>?> GetAllAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<AgencyDto>>("api/agencies", _json);

    public async Task<PagedResult<AgencyDto>?> GetPagedAsync(int page = 1, int pageSize = 10) =>
        await _http.GetFromJsonAsync<PagedResult<AgencyDto>>($"api/agencies/paged?page={page}&pageSize={pageSize}", _json);

    public async Task<AgencyDto?> CreateAsync(CreateAgencyDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/agencies", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgencyDto>(_json);
    }

    public async Task<AgencyDto?> UpdateAsync(Guid id, UpdateAgencyDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/agencies/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgencyDto>(_json);
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/agencies/{id}");
        response.EnsureSuccessStatusCode();
    }
}

public class BusinessClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public BusinessClient(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<BusinessDto?> GetByIdAsync(Guid id) =>
        await _http.GetFromJsonAsync<BusinessDto>($"api/businesses/{id}", _json);

    public async Task<IReadOnlyList<BusinessDto>?> GetAllAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<BusinessDto>>("api/businesses", _json);

    public async Task<PagedResult<BusinessDto>?> GetPagedAsync(int page = 1, int pageSize = 10) =>
        await _http.GetFromJsonAsync<PagedResult<BusinessDto>>($"api/businesses/paged?page={page}&pageSize={pageSize}", _json);

    public async Task<BusinessDto?> CreateAsync(CreateBusinessDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/businesses", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BusinessDto>(_json);
    }

    public async Task<BusinessDto?> UpdateAsync(Guid id, UpdateBusinessDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/businesses/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BusinessDto>(_json);
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/businesses/{id}");
        response.EnsureSuccessStatusCode();
    }
}
