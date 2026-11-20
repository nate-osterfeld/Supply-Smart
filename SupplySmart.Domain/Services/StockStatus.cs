using SupplySmart.Integrations.Models.Data;

namespace SupplySmart.Domain.Services;

internal static class StockStatus
{
    public static List<string> ForItem(InventoryItem item, DateTime now)
    {
        var statuses = new List<string>();
        if (item.Quantity == 0) statuses.Add("Out of Stock");
        else if (item.Quantity <= item.MinimumThreshold) statuses.Add("Low Stock");

        if (item.ExpirationDate <= now) statuses.Add("Expired");
        else if (item.ExpirationDate <= now.AddDays(30)) statuses.Add("Expiring Soon");

        if (statuses.Count == 0) statuses.Add("Safe");
        return statuses;
    }
}
