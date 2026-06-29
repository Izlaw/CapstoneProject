using Microsoft.AspNetCore.Components.Authorization;
using Supabase;
using System.Security.Claims;

namespace CapstoneProject.Services;

public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly Client _supabase;

    public SupabaseAuthStateProvider(Client supabase)
    {
        _supabase = supabase;
        _supabase.Auth.AddStateChangedListener((sender, state) => 
        {
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
            new Claim(ClaimTypes.NameIdentifier, user.Id),
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

        var identity = new ClaimsIdentity(claims, "SupabaseAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
