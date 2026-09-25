using Supabase;
using CapstoneProject.Models;
using Supabase.Gotrue;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;

namespace CapstoneProject.Services;

public class SupabaseService
{
    private readonly Supabase.Client _supabase;

    public SupabaseService(Supabase.Client supabase)
    {
        _supabase = supabase;
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

    public async Task<Dictionary<string, string>> GetCustomerNamesAsync(IEnumerable<string> customerIds)
    {
        var names = new Dictionary<string, string>();
        var ids = customerIds.Distinct().ToList();
        if (ids.Count == 0) return names;

        try
        {
            var response = await _supabase.From<UserProfileModel>()
                .Filter("id", Postgrest.Constants.Operator.In, ids)
                .Get();

                foreach (var profile in response.Models)
                {
                    names[profile.Id] = string.IsNullOrWhiteSpace(profile.FullName) ? "-" : profile.FullName;
                }
            
            return names;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return names;
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

    public async Task<Dictionary<string, string>> GetConversationNamesAsync(string conversationId)
    {
        try
        {
            var parameters = new Dictionary<string, object> { { "p_conversation_id", conversationId } };
            var names = await _supabase.Rpc<Dictionary<string, string>>("get_conversation_names", parameters);
            return names ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new Dictionary<string, string>();
        }
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

    public async Task<RealtimeChannel?> SubscribeToMessagesAsync(string conversationId, Action<ChatMessageModel> onMessage)
    {
        try
        {
            if (_supabase.Auth.CurrentSession?.AccessToken is string accessToken)
            {
                _supabase.Realtime.SetAuth(accessToken);
            }

            var channel = _supabase.Realtime.Channel($"messages:{conversationId}");
            channel.Register(new PostgresChangesOptions("public", "messages", PostgresChangesOptions.ListenType.Inserts, $"conversation_id=eq.{conversationId}"));
            channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.Inserts, (_, change) =>
            {
                var message = change.Model<ChatMessageModel>();
                if (message != null) onMessage(message);
            });
            await channel.Subscribe();
            return channel;
        }
        catch { return null; }
    }

    public void UnsubscribeFromMessages(RealtimeChannel channel)
    {
        try
        {
            _supabase.Realtime.Remove(channel);
        }
        catch { }
    }

    public async Task<Session?> LoginAsync(string email, string password)
    {
        var session = await _supabase.Auth.SignIn(email, password);
        await EnsureProfileExistsAsync(session?.User);
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
        await EnsureProfileExistsAsync(session?.User);
        return session;
    }

    private async Task EnsureProfileExistsAsync(Supabase.Gotrue.User? user)
    {
        if (user == null) return;
        
        var profile = await GetUserProfileAsync(user.Id);
        if (profile == null)
        {
            var newProfile = new UserProfileModel
            {
                Id = user.Id,
                FullName = user.UserMetadata.TryGetValue("full_name", out var fn) && fn != null ? fn.ToString() : user.Email,
                Role = "customer",
                CreatedAt = DateTime.UtcNow
            };
            try
            {
                await _supabase.From<UserProfileModel>().Insert(newProfile);
            }
            catch { /* Ignore if it already exists or fails */ }
        }
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

    public async Task<OperationResult> UpdateProfileAsync(string userId, string fullName, string? phone, string? address)
    {
        try
        {
            var response = await _supabase.From<UserProfileModel>()
                .Where(x => x.Id == userId)
                .Set(x => x.FullName!, fullName)
                .Set(x => x.Phone!, phone)
                .Set(x => x.Address!, address)
                .Update();

            if (response.Models.Count == 0)
            {
                return OperationResult.Failure("Your profile was not saved. Please log in again and try once more.");
            }

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult.Failure("Your profile could not be saved. Please try again.");
        }
    }

    public async Task<OperationResult> UpdateDisplayNameAsync(string fullName)
    {
        try
        {
            var attributes = new UserAttributes
            {
                Data = new Dictionary<string, object>
                {
                    { "full_name", fullName }
                }
            };
            await _supabase.Auth.Update(attributes);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult.Failure("Your name was saved, but the menu will show the old name until you log in again.");
        }
    }

    public async Task LogoutAsync()
    {
        await _supabase.Auth.SignOut();
    }
}
