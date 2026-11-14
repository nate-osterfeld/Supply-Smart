using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services;

public interface IForecastService
{
    Task<ForecastResponseDto> GetForecastAsync(
        int organizationId,
        int forecastDays
    );
}

public class ForecastService : IForecastService
{
    private readonly SupplySmartDbContext _context;

    /*
     * For the baseline model we will look at the
     * previous 30 calendar days of checkout activity.
     */
    private const int LookbackDays = 30;

    public ForecastService(SupplySmartDbContext context)
    {
        _context = context;
    }

    public async Task<ForecastResponseDto> GetForecastAsync(
        int organizationId,
        int forecastDays
    )
    {
        /*
         * Only allow the forecast periods currently
         * supported by the Schedule Forecast UI.
         */
        if (forecastDays != 7 &&
            forecastDays != 14 &&
            forecastDays != 30)
        {
            forecastDays = 7;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-LookbackDays);

        /*
         * Load current inventory for this organization.
         */
        var inventoryItems = await _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.OrganizationId == organizationId)
            .OrderBy(i => i.Name)
            .ToListAsync();

        /*
         * Only checkout transactions represent demand.
         *
         * Restocks are deliberately excluded because
         * receiving inventory is not inventory usage.
         */
        var checkoutTransactions =
            await _context.InventoryTransactions
                .AsNoTracking()
                .Where(t =>
                    t.OrganizationId == organizationId &&
                    t.TransactionType == "checkout" &&
                    t.Timestamp >= cutoffDate &&
                    t.QuantityChange < 0
                )
                .ToListAsync();

        var forecasts = new List<InventoryForecastDto>();

        foreach (var item in inventoryItems)
        {
            /*
             * Get checkout history belonging to this item.
             */
            var itemCheckouts = checkoutTransactions
                .Where(t => t.InventoryItemId == item.Id)
                .ToList();

            /*
             * Checkout values are negative:
             *
             * -5 means 5 units were consumed.
             *
             * Math.Abs converts this to actual demand.
             */
            var totalHistoricalUsage = itemCheckouts
                .Sum(t => Math.Abs(t.QuantityChange));

            /*
             * Baseline forecast:
             *
             * average daily usage =
             * units consumed during lookback period
             * divided by number of calendar days.
             */
            var averageDailyUsage =
                totalHistoricalUsage > 0
                    ? (double)totalHistoricalUsage / LookbackDays
                    : 0;

            var predictedDemand = (int)Math.Ceiling(
                averageDailyUsage * forecastDays
            );

            var projectedQuantity =
                item.Quantity - predictedDemand;

            string riskLevel;

            if (projectedQuantity <= 0)
            {
                riskLevel = "Critical";
            }
            else if (
                projectedQuantity <= item.MinimumThreshold
            )
            {
                riskLevel = "Medium";
            }
            else
            {
                riskLevel = "Low";
            }

            /*
             * Order enough inventory to cover expected
             * demand while finishing at the minimum
             * threshold.
             */
            var recommendedOrderQuantity = Math.Max(
                0,
                predictedDemand +
                item.MinimumThreshold -
                item.Quantity
            );

            /*
             * Be transparent when no historical usage
             * exists yet.
             */
            var forecastMethod =
                itemCheckouts.Count > 0
                    ? "30-day average checkout baseline"
                    : "Insufficient checkout history";

            forecasts.Add(
                new InventoryForecastDto
                {
                    InventoryItemId = item.Id,
                    ItemName = item.Name,
                    Sku = item.Sku,

                    CurrentQuantity = item.Quantity,
                    MinimumThreshold =
                        item.MinimumThreshold,

                    ForecastDays = forecastDays,

                    AverageDailyUsage =
                        Math.Round(averageDailyUsage, 2),

                    PredictedDemand = predictedDemand,

                    ProjectedQuantity =
                        projectedQuantity,

                    RiskLevel = riskLevel,

                    RecommendedOrderQuantity =
                        recommendedOrderQuantity,

                    HistoricalCheckoutCount =
                        itemCheckouts.Count,

                    ForecastMethod = forecastMethod
                }
            );
        }

        return new ForecastResponseDto
        {
            ForecastDays = forecastDays,

            LookbackDays = LookbackDays,

            TotalPredictedDemand =
                forecasts.Sum(x => x.PredictedDemand),

            ItemsAtRisk =
                forecasts.Count(
                    x => x.RiskLevel != "Low"
                ),

            PotentialStockouts =
                forecasts.Count(
                    x => x.ProjectedQuantity <= 0
                ),

            SuggestedOrders =
                forecasts.Count(
                    x =>
                        x.RecommendedOrderQuantity > 0
                ),

            Items = forecasts
        };
    }
}