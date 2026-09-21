namespace CapstoneProject.Models;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "In Progress";
    public const string ReadyForPickup = "Ready for Pickup";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Pending, InProgress, ReadyForPickup, Completed, Cancelled };
}
