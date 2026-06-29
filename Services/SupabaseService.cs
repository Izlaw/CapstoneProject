using System.Net.Http.Json;

namespace CapstoneProject.Services;

public class SupabaseService
{
    public const string ProjectUrl = "https://jvljcrwazmlcjkqomwdz.supabase.co";
    public const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imp2bGpjcndhem1sY2prcW9td2R6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzUxODM4MzYsImV4cCI6MjA5MDc1OTgzNn0.siWFE9V5QhJbNdCxq6U4wvfXhmemt1YzG6mx9p_pbO8";

    private readonly HttpClient _http;

    public SupabaseService()
    {
        _http = new HttpClient();
        _http.BaseAddress = new Uri(ProjectUrl);
        _http.DefaultRequestHeaders.Add("apikey", AnonKey);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {AnonKey}");
    }

    /// <summary>
    /// Save a design record to the "designs" table.
    /// </summary>
    public async Task<bool> SaveDesignAsync(string name, string shirtColor, string? textOverlay, string? textureUrl)
    {
        try
        {
            var payload = new
            {
                name,
                shirt_color = shirtColor,
                text_overlay = textOverlay,
                texture_url = textureUrl
            };

            var response = await _http.PostAsJsonAsync("/rest/v1/designs", payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Fetch all designs from the "designs" table.
    /// </summary>
    public async Task<List<SupabaseDesignDto>> GetDesignsAsync()
    {
        try
        {
            // Fetch all designs, ordered by created_at descending
            var response = await _http.GetFromJsonAsync<List<SupabaseDesignDto>>("/rest/v1/designs?select=*&order=created_at.desc");
            return response ?? new List<SupabaseDesignDto>();
        }
        catch
        {
            return new List<SupabaseDesignDto>();
        }
    }

    public void SetAuthToken(string? token)
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
        else
        {
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {AnonKey}");
        }
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        var payload = new { email, password };
        var response = await _http.PostAsJsonAsync("/auth/v1/token?grant_type=password", payload);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Login failed: {errorContent}");
    }

    public async Task<AuthResponse?> RegisterAsync(string email, string password)
    {
        var payload = new { email, password };
        var response = await _http.PostAsJsonAsync("/auth/v1/signup", payload);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Signup failed: {errorContent}");
    }
}

public class AuthResponse
{
    public string access_token { get; set; } = "";
    public string token_type { get; set; } = "";
    public int expires_in { get; set; }
    public string refresh_token { get; set; } = "";
    public SupabaseUser user { get; set; } = new();
}

public class SupabaseUser
{
    public string id { get; set; } = "";
    public string email { get; set; } = "";
}

public class SupabaseDesignDto
{
    public Guid id { get; set; }
    public string name { get; set; } = "";
    public string shirt_color { get; set; } = "";
    public string? text_overlay { get; set; }
    public string? texture_url { get; set; }
    public DateTime created_at { get; set; }
}
