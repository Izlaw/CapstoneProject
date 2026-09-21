using Microsoft.AspNetCore.Components.Authorization;
using Supabase;
using System.Security.Claims;
using CapstoneProject.Models;

namespace CapstoneProject.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly Client _supabase;
    private string? _cachedRoleUserId;
    private string? _cachedRole;

    public SupabaseAuthStateProvider(Client supabase)
    {
        _supabase = supabase;
        _supabase.Auth.AddStateChangedListener((sender, state) =>
        {
            ClearCachedRole();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        });
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = _supabase.Auth.CurrentSession;
        var user = _supabase.Auth.CurrentUser;

        if (session == null || user == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        };

        if (user.UserMetadata.TryGetValue("full_name", out var nameObj) && nameObj != null)
        {
            claims.Add(new Claim(ClaimTypes.Name, nameObj.ToString()!));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Name, user.Email ?? ""));
        }

        var role = await GetRoleAsync(user.Id);
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "SupabaseAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private async Task<string?> GetRoleAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        if (_cachedRoleUserId == userId) return _cachedRole;

        try
        {
            var profile = await _supabase.From<UserProfileModel>()
                .Where(x => x.Id == userId)
                .Single();

            if (profile == null) return null;

            _cachedRoleUserId = userId;
            _cachedRole = profile.Role;
            return _cachedRole;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private void ClearCachedRole()
    {
        _cachedRoleUserId = null;
        _cachedRole = null;
    }
}
