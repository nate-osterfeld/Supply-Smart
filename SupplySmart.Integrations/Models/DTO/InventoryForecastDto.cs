namespace SupplySmart.Integrations.Models.DTO;

public class InventoryForecastDto
{
    public int InventoryItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int CurrentQuantity { get; set; }

    public int MinimumThreshold { get; set; }

    public int ForecastDays { get; set; }

    public double AverageDailyUsage { get; set; }

    public int PredictedDemand { get; set; }

    public int ProjectedQuantity { get; set; }

    public string RiskLevel { get; set; } = string.Empty;

    public int RecommendedOrderQuantity { get; set; }

    public int HistoricalCheckoutCount { get; set; }

    public string ForecastMethod { get; set; } = string.Empty;
}