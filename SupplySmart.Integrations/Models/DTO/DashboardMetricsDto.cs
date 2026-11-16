namespace SupplySmart.Integrations.Models.DTO;

public class DashboardMetricsDto
{
    public decimal TotalInventoryValue { get; set; }
    public long ExpiringSoonCount { get; set; }
    public long ActiveLowStockAlertsCount { get; set; }
    public int PendingRestocksCount { get; set; }
    public int SafeStockPercentage { get; set; }
    public int ExpiringSoonPercentage { get; set; }
    public int ExpiredPercentage { get; set; }
}
