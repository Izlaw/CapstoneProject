using CapstoneProject.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Postgrest.Exceptions;
using Supabase;

namespace CapstoneProject.Services;

public class OrderService
{
    private const string OrderImagesBucket = "order-images";
    private const int SignedUrlSeconds = 3600;
    private readonly Client _supabase;

    public OrderService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<OperationResult<OrderQuoteModel>> GetQuoteAsync(OrderRequestModel request)
    {
        try
        {
            var quote = await _supabase.Rpc<OrderQuoteModel>("quote_order", BuildQuoteParameters(request));
            if (quote == null)
            {
                return OperationResult<OrderQuoteModel>.Failure("The price could not be calculated.");
            }

            return OperationResult<OrderQuoteModel>.Success(quote);
        }
        catch (PostgrestException ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<OrderQuoteModel>.Failure(GetReadableMessage(ex));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<OrderQuoteModel>.Failure("The price could not be calculated. Please try again.");
        }
    }

    public async Task<OperationResult<string>> PlaceOrderAsync(OrderRequestModel request)
    {
        try
        {
            var parameters = BuildQuoteParameters(request);
            parameters["p_shirt_color"] = request.ShirtColor;
            parameters["p_design_data"] = request.DesignData;
            parameters["p_image_path"] = request.ImagePath;
            parameters["p_original_file_name"] = request.OriginalFileName;

            var orderId = await _supabase.Rpc<string>("place_order", parameters);
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return OperationResult<string>.Failure("Your order could not be placed. Please try again.");
            }

            return OperationResult<string>.Success(orderId.Trim('"'));
        }
        catch (PostgrestException ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<string>.Failure(GetReadableMessage(ex));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<string>.Failure("Your order could not be placed. Please try again.");
        }
    }

    public async Task<OperationResult<string>> UploadImageAsync(byte[] fileBytes, string contentType)
    {
        try
        {
            var userId = _supabase.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                return OperationResult<string>.Failure("Sign in to upload images.");
            }

            var extension = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
            var filePath = $"{userId}/{Guid.NewGuid():N}{extension}";

            await _supabase.Storage.From(OrderImagesBucket)
                .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions { ContentType = contentType });

            return OperationResult<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<string>.Failure("The image could not be uploaded. Please try again.");
        }
    }

    public async Task<string?> GetImageUrlAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return null;

        try
        {
            return await _supabase.Storage.From(OrderImagesBucket).CreateSignedUrl(imagePath, SignedUrlSeconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task DeleteImagesAsync(List<string> imagePaths)
    {
        if (imagePaths.Count == 0) return;

        try
        {
            await _supabase.Storage.From(OrderImagesBucket).Remove(imagePaths);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task<OperationResult<AppOrderModel>> GetOrderDetailsAsync(string orderId)
    {
        try
        {
            var order = await _supabase.From<AppOrderModel>()
                .Select("*, custom_orders(*), collection_orders(*), upload_orders(*), order_items(*)")
                .Where(x => x.Id == orderId)
                .Single();

            if (order == null)
            {
                return OperationResult<AppOrderModel>.Failure("This order could not be found.");
            }

            return OperationResult<AppOrderModel>.Success(order);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<AppOrderModel>.Failure("This order could not be loaded. Please try again.");
        }
    }

    public async Task<OperationResult<string>> GetShareTokenAsync(string orderId)
    {
        try
        {
            var order = await _supabase.From<AppOrderModel>()
                .Where(x => x.Id == orderId)
                .Single();

            if (order == null || string.IsNullOrEmpty(order.ShareToken))
            {
                return OperationResult<string>.Failure("The share link could not be found.");
            }

            return OperationResult<string>.Success(order.ShareToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<string>.Failure("The share link could not be loaded. Please try again.");
        }
    }

    public async Task<OperationResult<SharedOrderModel>> GetSharedOrderAsync(Guid token)
    {
        try
        {
            var options = new Supabase.Functions.Client.InvokeFunctionOptions
            {
                Body = new Dictionary<string, object> { { "token", token.ToString() } }
            };
            var json = await _supabase.Functions.Invoke("shared-order", options: options);

            var order = JsonConvert.DeserializeObject<SharedOrderModel>(json);
            if (order == null)
            {
                return OperationResult<SharedOrderModel>.Failure("This order could not be found.");
            }

            return OperationResult<SharedOrderModel>.Success(order);
        }
        catch (Supabase.Functions.Exceptions.FunctionsException ex)
        {
            Console.WriteLine(ex.Message);
            var message = ex.StatusCode == 404 || ex.StatusCode == 400
                ? "This link is not valid, or the order no longer exists."
                : "This order could not be loaded. Please try again.";
            return OperationResult<SharedOrderModel>.Failure(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<SharedOrderModel>.Failure("This order could not be loaded. Please try again.");
        }
    }

    public async Task<OperationResult> UpdateOrderStatusAsync(string orderId, string status)
    {
        try
        {
            var update = await _supabase.From<AppOrderModel>()
                .Where(x => x.Id == orderId)
                .Set(x => x.Status, status)
                .Update();

            if (update.Models.Count == 0)
            {
                return OperationResult.Failure("The order status could not be updated.");
            }

            return OperationResult.Success();
        }
        catch (PostgrestException ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult.Failure(GetReadableMessage(ex));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult.Failure("The order status could not be updated. Please try again.");
        }
    }

    private static Dictionary<string, object?> BuildQuoteParameters(OrderRequestModel request)
    {
        return new Dictionary<string, object?>
        {
            { "p_order_type", request.OrderType },
            { "p_items", request.Items },
            { "p_fabric_id", request.FabricId },
            { "p_fabric_note", request.FabricNote },
            { "p_collection_id", request.CollectionId },
            { "p_timeframe_id", request.TimeframeId }
        };
    }

    private static string GetReadableMessage(PostgrestException ex)
    {
        try
        {
            var content = JObject.Parse(ex.Content ?? ex.Message);
            var code = content["code"]?.ToString();
            var message = content["message"]?.ToString();

            var isFriendlyCode = code == "22023" || code == "28000" || code == "23503" || code == "42501";
            if (isFriendlyCode && !string.IsNullOrWhiteSpace(message))
            {
                return message;
            }
        }
        catch (Exception)
        {
        }

        return "Something went wrong. Please try again.";
    }
}
