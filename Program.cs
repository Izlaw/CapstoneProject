using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Blazored.LocalStorage;
using CapstoneProject;
using CapstoneProject.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor
builder.Services.AddMudServices();

// Blazored LocalStorage for session persistence
builder.Services.AddBlazoredLocalStorage();

// Auth
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, CapstoneProject.Services.SupabaseAuthStateProvider>();

// App Services
builder.Services.AddScoped<DesignService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<SupabaseService>();

var supabaseUrl = "https://jvljcrwazmlcjkqomwdz.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imp2bGpjcndhem1sY2prcW9td2R6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzUxODM4MzYsImV4cCI6MjA5MDc1OTgzNn0.siWFE9V5QhJbNdCxq6U4wvfXhmemt1YzG6mx9p_pbO8";

builder.Services.AddScoped<Supabase.Client>(sp =>
{
    var localStorage = sp.GetRequiredService<Blazored.LocalStorage.ISyncLocalStorageService>();
    var supabaseOptions = new Supabase.SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true,
        SessionHandler = new CapstoneProject.Services.BlazorSessionHandler(localStorage)
    };
    return new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions);
});

var host = builder.Build();

// Initialize Supabase BEFORE running the app so it restores the session from local storage.
// Without this, CurrentSession is always null on page load and auth always fails.
var supabaseClient = host.Services.GetRequiredService<Supabase.Client>();
await supabaseClient.InitializeAsync();

await host.RunAsync();
