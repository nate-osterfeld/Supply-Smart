namespace SupplySmart.Integrations.Models.DTO;

public class DashboardMetricsDto
{
    public decimal TotalInventoryValue { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ActiveLowStockAlertsCount { get; set; }
    public int PendingRestocksCount { get; set; }
}
