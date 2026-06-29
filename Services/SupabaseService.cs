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
    public async Task<string?> PlaceOrderAsync(string name, string shirtColor, string? textOverlay, string? textureUrl, string size, string fabric, int quantity)
    {
        try
        {
            var order = new AppOrderModel
            {
                OrderType = "custom",
                Status = "Pending",
                TotalPrice = quantity * 1250, // Dummy calculation for custom design price
                DesignReference = textureUrl,
                FabricType = fabric,
                CreatedAt = DateTime.UtcNow
            };
            
            // Set the customer ID if logged in
            if (_supabase.Auth.CurrentUser != null)
            {
                order.CustomerId = _supabase.Auth.CurrentUser.Id;
            }

            var response = await _supabase.From<AppOrderModel>().Insert(order);
            return response.Models.FirstOrDefault()?.Id;
        }
        catch
        {
            return null;
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

    public async Task<List<AppOrderModel>> GetAllOrdersAsync()
    {
        try
        {
            var response = await _supabase.From<AppOrderModel>()
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();
            return response.Models;
        }
        catch
        {
            return new List<AppOrderModel>();
        }
    }

    public async Task<List<AppOrderModel>> GetCustomerOrdersAsync(string customerId)
    {
        try
        {
            var response = await _supabase.From<AppOrderModel>()
                .Where(x => x.CustomerId == customerId)
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();
            return response.Models;
        }
        catch
        {
            return new List<AppOrderModel>();
        }
    }

    public async Task<AppOrderModel?> GetOrderByIdAsync(string orderId)
    {
        try
        {
            var response = await _supabase.From<AppOrderModel>()
                .Where(x => x.Id == orderId)
                .Single();
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
    {
        try
        {
            var update = await _supabase.From<AppOrderModel>()
                .Where(x => x.Id == orderId)
                .Set(x => x.Status, status)
                .Update();
            return update.Models.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetOrCreateConversationAsync(string customerId)
    {
        try
        {
            var convo = await _supabase.From<ConversationModel>()
                .Where(x => x.CustomerId == customerId)
                .Single();
            if (convo != null) return convo.Id;
        }
        catch { /* Not found */ }

        try
        {
            var newConvo = new ConversationModel { CustomerId = customerId, Status = "open", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var response = await _supabase.From<ConversationModel>().Insert(newConvo);
            return response.Models.FirstOrDefault()?.Id;
        }
        catch { return null; }
    }

    public async Task<List<ChatMessageModel>> GetMessagesAsync(string conversationId)
    {
        try
        {
            var response = await _supabase.From<ChatMessageModel>()
                .Where(x => x.ConversationId == conversationId)
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Ascending)
                .Get();
            return response.Models;
        }
        catch { return new List<ChatMessageModel>(); }
    }

    public async Task<bool> SendMessageAsync(string conversationId, string content)
    {
        try
        {
            if (_supabase.Auth.CurrentUser == null) return false;
            
            var msg = new ChatMessageModel
            {
                ConversationId = conversationId,
                SenderId = _supabase.Auth.CurrentUser.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            var response = await _supabase.From<ChatMessageModel>().Insert(msg);
            return response.Models.Count > 0;
        }
        catch { return false; }
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

    public async Task<UserProfileModel?> GetUserProfileAsync(string userId)
    {
        try
        {
            var response = await _supabase.From<UserProfileModel>()
                .Where(x => x.Id == userId)
                .Single();
            
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _supabase.Auth.SignOut();
    }
}
