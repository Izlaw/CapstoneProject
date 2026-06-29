using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using CapstoneProject;
using CapstoneProject.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor
builder.Services.AddMudServices();

// Auth
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, CapstoneProject.Services.SupabaseAuthStateProvider>();

// App Services
builder.Services.AddScoped<DesignService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<SupabaseService>();

var supabaseUrl = "https://jvljcrwazmlcjkqomwdz.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imp2bGpjcndhem1sY2prcW9td2R6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzUxODM4MzYsImV4cCI6MjA5MDc1OTgzNn0.siWFE9V5QhJbNdCxq6U4wvfXhmemt1YzG6mx9p_pbO8";
var supabaseOptions = new Supabase.SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true
};
builder.Services.AddScoped<Supabase.Client>(_ => new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions));

await builder.Build().RunAsync();
