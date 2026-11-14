namespace SupplySmart.Integrations.Models.DTO;

public class ForecastResponseDto
{
    public int ForecastDays { get; set; }

    public int LookbackDays { get; set; }

    public int TotalPredictedDemand { get; set; }

    public int ItemsAtRisk { get; set; }

    public int PotentialStockouts { get; set; }

    public int SuggestedOrders { get; set; }

    public List<InventoryForecastDto> Items { get; set; } = new();
}