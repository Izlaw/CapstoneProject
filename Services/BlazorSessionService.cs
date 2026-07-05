using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Blazored.LocalStorage;
using System.Text.Json;

namespace CapstoneProject.Services;

public class BlazorSessionHandler : IGotrueSessionPersistence<Session>
{
    private readonly ISyncLocalStorageService _localStorage;
    private const string SESSION_KEY = "SUPABASE_SESSION";

    public BlazorSessionHandler(ISyncLocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public void DestroySession()
    {
        _localStorage.RemoveItem(SESSION_KEY);
    }

    public Session? LoadSession()
    {
        try
        {
            var json = _localStorage.GetItemAsString(SESSION_KEY);
            if (string.IsNullOrEmpty(json)) return null;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Session>(json);
        }
        catch
        {
            return null;
        }
    }

    public void SaveSession(Session session)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(session);
        _localStorage.SetItemAsString(SESSION_KEY, json);
    }
}
