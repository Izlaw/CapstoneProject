using Supabase;
using CapstoneProject.Models;
using Supabase.Gotrue;

namespace CapstoneProject.Services;

public class SupabaseService
{
    private readonly Supabase.Client _supabase;

    public SupabaseService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    /// <summary>
    /// Save a design record to the orders table.
    /// </summary>
    public async Task<bool> SaveDesignAsync(string name, string shirtColor, string? textOverlay, string? textureUrl)
    {
        try
        {
            // Note: Since we consolidated, saving a design is now placing an order.
            // But if this is just a raw save (before order placement), we might save it as a pending order.
            // For now, mapping to the new AppOrderModel:
            var order = new AppOrderModel
            {
                OrderType = "custom",
                Status = "Pending",
                TotalPrice = 0, // Placeholder
                DesignReference = textureUrl,
                CreatedAt = DateTime.UtcNow
            };
            
            // Set the customer ID if logged in
            if (_supabase.Auth.CurrentUser != null)
            {
                order.CustomerId = _supabase.Auth.CurrentUser.Id;
            }

            var response = await _supabase.From<AppOrderModel>().Insert(order);
            return response.Models.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Fetch all custom orders from the orders table.
    /// </summary>
    public async Task<List<AppOrderModel>> GetDesignsAsync()
    {
        try
        {
            var response = await _supabase.From<AppOrderModel>()
                .Where(x => x.OrderType == "custom")
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();
                
            return response.Models;
        }
        catch
        {
            return new List<AppOrderModel>();
        }
    }

    public async Task<Session?> LoginAsync(string email, string password)
    {
        var session = await _supabase.Auth.SignIn(email, password);
        return session;
    }

    public async Task<Session?> RegisterAsync(string email, string password, string fullName)
    {
        var options = new SignUpOptions
        {
            Data = new Dictionary<string, object>
            {
                { "full_name", fullName }
            }
        };
        var session = await _supabase.Auth.SignUp(email, password, options);
        return session;
    }

    public async Task LogoutAsync()
    {
        await _supabase.Auth.SignOut();
    }
}
