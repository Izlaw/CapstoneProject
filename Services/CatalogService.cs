using CapstoneProject.Models;
using Postgrest.Exceptions;
using Postgrest.Models;
using Supabase;
using static Postgrest.Constants;

namespace CapstoneProject.Services;

public class CatalogService
{
    private const string CollectionImagesBucket = "collection-images";
    private readonly Client _supabase;

    public CatalogService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<SizeModel>> GetSizesAsync()
    {
        var response = await _supabase.From<SizeModel>()
            .Order(x => x.SortOrder, Ordering.Ascending)
            .Order(x => x.Name, Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public async Task<List<FabricModel>> GetFabricsAsync()
    {
        var response = await _supabase.From<FabricModel>()
            .Order(x => x.SortOrder, Ordering.Ascending)
            .Order(x => x.Name, Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public async Task<List<TimeframeModel>> GetTimeframesAsync()
    {
        var response = await _supabase.From<TimeframeModel>()
            .Order(x => x.SortOrder, Ordering.Ascending)
            .Order(x => x.Label, Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public async Task<List<CollectionItemModel>> GetCollectionsAsync()
    {
        var response = await _supabase.From<CollectionItemModel>()
            .Order(x => x.SortOrder, Ordering.Ascending)
            .Order(x => x.Name, Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public async Task<List<PriceAuditLogModel>> GetPriceHistoryAsync(int limit = 200)
    {
        var response = await _supabase.From<PriceAuditLogModel>()
            .Order(x => x.ChangedAt, Ordering.Descending)
            .Limit(limit)
            .Get();
        return response.Models;
    }

    public async Task<Dictionary<string, string>> GetProfileNamesAsync(IEnumerable<string> userIds)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<string, string>();

        var response = await _supabase.From<UserProfileModel>()
            .Filter(x => x.Id, Operator.In, idList)
            .Get();

        return response.Models.ToDictionary(profile => profile.Id, profile => profile.FullName ?? "-");
    }

    public async Task<OperationResult> SaveAsync<T>(T model, bool isNew) where T : BaseModel, new()
    {
        try
        {
            if (isNew)
            {
                await _supabase.From<T>().Insert(model);
                return OperationResult.Success();
            }

            var response = await _supabase.From<T>().Update(model);
            if (response.Models.Count == 0)
            {
                return OperationResult.Failure("Your changes were not saved. You may not have permission to edit this item.");
            }

            return OperationResult.Success();
        }
        catch (PostgrestException ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult.Failure(GetFriendlyMessage(ex));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult.Failure("Something went wrong. Please try again.");
        }
    }

    public async Task<OperationResult> MoveAsync<T>(List<T> allItems, T item, T neighbor) where T : BaseModel, ISortableItem, new()
    {
        try
        {
            var reordered = allItems.ToList();
            var itemIndex = reordered.FindIndex(candidate => ReferenceEquals(candidate, item));
            var neighborIndex = reordered.FindIndex(candidate => ReferenceEquals(candidate, neighbor));
            if (itemIndex < 0 || neighborIndex < 0)
            {
                return OperationResult.Failure("The item could not be found. Please refresh the page.");
            }

            (reordered[itemIndex], reordered[neighborIndex]) = (reordered[neighborIndex], reordered[itemIndex]);

            for (var position = 0; position < reordered.Count; position++)
            {
                var newOrder = (position + 1) * 10;
                if (reordered[position].SortOrder == newOrder) continue;

                reordered[position].SortOrder = newOrder;
                var saved = await SaveAsync(reordered[position], false);
                if (!saved.IsSuccess) return saved;
            }

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult.Failure("The order could not be changed. Please try again.");
        }
    }

    public async Task<OperationResult<string>> UploadCollectionImageAsync(byte[] fileBytes, string contentType)
    {
        try
        {
            var extension = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
            var filePath = $"{Guid.NewGuid():N}{extension}";

            var bucket = _supabase.Storage.From(CollectionImagesBucket);
            await bucket.Upload(fileBytes, filePath, new Supabase.Storage.FileOptions { ContentType = contentType });

            return OperationResult<string>.Success(bucket.GetPublicUrl(filePath));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return OperationResult<string>.Failure("The image could not be uploaded. Please try again.");
        }
    }

    private static string GetFriendlyMessage(PostgrestException ex)
    {
        var message = ex.Message ?? string.Empty;

        if (message.Contains("23505") || message.Contains("duplicate key"))
        {
            return "An item with this name already exists.";
        }

        if (message.Contains("23514") || message.Contains("check constraint"))
        {
            return "One of the values is not allowed. Please check the numbers.";
        }

        if (message.Contains("42501") || message.Contains("row-level security"))
        {
            return "You do not have permission to do this.";
        }

        return "Your changes could not be saved. Please try again.";
    }
}
