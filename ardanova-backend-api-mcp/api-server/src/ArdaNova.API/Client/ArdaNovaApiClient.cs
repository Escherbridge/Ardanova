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
    public GuildClient Guilds => new(_httpClient, _jsonOptions);
    public ShopClient Shops => new(_httpClient, _jsonOptions);

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

    public async Task<UserDto?> GetByIdAsync(string id) =>
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

    public async Task<UserDto?> UpdateAsync(string id, UpdateUserDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/users/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(_json);
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/users/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserDto?> VerifyAsync(string id)
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

    public async Task<ProjectDto?> GetByIdAsync(string id) =>
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

    public async Task<ProjectDto?> UpdateAsync(string id, UpdateProjectDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/projects/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectDto>(_json);
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/projects/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ProjectDto?> PublishAsync(string id)
    {
        var response = await _http.PostAsync($"api/projects/{id}/publish", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectDto>(_json);
    }
}

public class GuildClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public GuildClient(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<GuildDto?> GetByIdAsync(string id) =>
        await _http.GetFromJsonAsync<GuildDto>($"api/guilds/{id}", _json);

    public async Task<IReadOnlyList<GuildDto>?> GetAllAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<GuildDto>>("api/guilds", _json);

    public async Task<PagedResult<GuildDto>?> GetPagedAsync(int page = 1, int pageSize = 10) =>
        await _http.GetFromJsonAsync<PagedResult<GuildDto>>($"api/guilds/paged?page={page}&pageSize={pageSize}", _json);

    public async Task<GuildDto?> GetBySlugAsync(string slug) =>
        await _http.GetFromJsonAsync<GuildDto>($"api/guilds/slug/{slug}", _json);

    public async Task<GuildDto?> CreateAsync(CreateGuildDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/guilds", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GuildDto>(_json);
    }

    public async Task<GuildDto?> UpdateAsync(string id, UpdateGuildDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/guilds/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GuildDto>(_json);
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/guilds/{id}");
        response.EnsureSuccessStatusCode();
    }
}

public class ShopClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public ShopClient(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    public async Task<ShopDto?> GetByIdAsync(string id) =>
        await _http.GetFromJsonAsync<ShopDto>($"api/shops/{id}", _json);

    public async Task<IReadOnlyList<ShopDto>?> GetAllAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<ShopDto>>("api/shops", _json);

    public async Task<PagedResult<ShopDto>?> GetPagedAsync(int page = 1, int pageSize = 10) =>
        await _http.GetFromJsonAsync<PagedResult<ShopDto>>($"api/shops/paged?page={page}&pageSize={pageSize}", _json);

    public async Task<IReadOnlyList<ShopDto>?> GetByOwnerIdAsync(string ownerId) =>
        await _http.GetFromJsonAsync<IReadOnlyList<ShopDto>>($"api/shops/owner/{ownerId}", _json);

    public async Task<ShopDto?> CreateAsync(CreateShopDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/shops", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShopDto>(_json);
    }

    public async Task<ShopDto?> UpdateAsync(string id, UpdateShopDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/shops/{id}", dto, _json);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShopDto>(_json);
    }

    public async Task DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/shops/{id}");
        response.EnsureSuccessStatusCode();
    }
}
