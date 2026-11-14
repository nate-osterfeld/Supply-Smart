import { useEffect, useState } from 'react';
import { ForecastService } from '../../api/ForecastService';
import type {
  ForecastResponseDto,
  InventoryForecastDto,
} from '../../models';

const forecastService = new ForecastService();

export default function ForecastPage() {
  const [forecastDays, setForecastDays] = useState(7);
  const [forecast, setForecast] =
    useState<ForecastResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadForecast = async () => {
      try {
        setLoading(true);
        setError(null);

        const data = await forecastService.getForecast(
          forecastDays
        );

        setForecast(data);
      } catch (err) {
        console.error(
          'Failed to load inventory forecast:',
          err
        );

        setError(
          'Unable to load forecast data. Please try again.'
        );
      } finally {
        setLoading(false);
      }
    };

    loadForecast();
  }, [forecastDays]);

  const getRiskClasses = (
    riskLevel: InventoryForecastDto['riskLevel']
  ) => {
    switch (riskLevel) {
      case 'Critical':
        return 'bg-red-100 text-red-700';
      case 'Medium':
        return 'bg-yellow-100 text-yellow-700';
      default:
        return 'bg-green-100 text-green-700';
    }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">
            Schedule Forecast
          </h1>

          <p className="mt-1 text-sm text-gray-500">
            Forecast inventory demand using recent checkout
            history.
          </p>
        </div>

        <div className="flex gap-2">
          {[7, 14, 30].map((days) => (
            <button
              key={days}
              type="button"
              onClick={() => setForecastDays(days)}
              className={`rounded-lg px-4 py-2 text-sm font-medium transition ${
                forecastDays === days
                  ? 'bg-blue-600 text-white'
                  : 'border border-gray-300 bg-white text-gray-700 hover:bg-gray-50'
              }`}
            >
              {days} Days
            </button>
          ))}
        </div>
      </div>

      {loading && (
        <div className="rounded-lg border border-gray-200 bg-white p-8 text-center text-gray-500">
          Loading forecast...
        </div>
      )}

      {error && !loading && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-700">
          {error}
        </div>
      )}

      {!loading && !error && forecast && (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-xl border border-gray-200 bg-white p-5">
              <p className="text-sm font-medium text-gray-500">
                Predicted Demand
              </p>

              <p className="mt-2 text-3xl font-semibold text-gray-900">
                {forecast.totalPredictedDemand}
              </p>

              <p className="mt-1 text-xs text-gray-500">
                Next {forecast.forecastDays} days
              </p>
            </div>

            <div className="rounded-xl border border-gray-200 bg-white p-5">
              <p className="text-sm font-medium text-gray-500">
                Items at Risk
              </p>

              <p className="mt-2 text-3xl font-semibold text-gray-900">
                {forecast.itemsAtRisk}
              </p>

              <p className="mt-1 text-xs text-gray-500">
                Medium or critical risk
              </p>
            </div>

            <div className="rounded-xl border border-gray-200 bg-white p-5">
              <p className="text-sm font-medium text-gray-500">
                Potential Stockouts
              </p>

              <p className="mt-2 text-3xl font-semibold text-gray-900">
                {forecast.potentialStockouts}
              </p>

              <p className="mt-1 text-xs text-gray-500">
                Projected quantity at or below zero
              </p>
            </div>

            <div className="rounded-xl border border-gray-200 bg-white p-5">
              <p className="text-sm font-medium text-gray-500">
                Suggested Orders
              </p>

              <p className="mt-2 text-3xl font-semibold text-gray-900">
                {forecast.suggestedOrders}
              </p>

              <p className="mt-1 text-xs text-gray-500">
                Items needing replenishment
              </p>
            </div>
          </div>

          <div className="overflow-hidden rounded-xl border border-gray-200 bg-white">
            <div className="border-b border-gray-200 px-5 py-4">
              <h2 className="font-semibold text-gray-900">
                Inventory Forecast
              </h2>

              <p className="mt-1 text-sm text-gray-500">
                Based on up to {forecast.lookbackDays} days of
                checkout activity.
              </p>
            </div>

            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Item
                    </th>

                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Current
                    </th>

                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Minimum
                    </th>

                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Avg Daily Usage
                    </th>

                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Predicted Demand
                    </th>

                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Projected
                    </th>

                    <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Risk
                    </th>

                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Recommended Order
                    </th>

                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500">
                      Forecast Method
                    </th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-gray-100">
                  {forecast.items.map((item) => (
                    <tr
                      key={item.inventoryItemId}
                      className="hover:bg-gray-50"
                    >
                      <td className="px-4 py-4">
                        <p className="font-medium text-gray-900">
                          {item.itemName}
                        </p>

                        <p className="text-xs text-gray-500">
                          {item.sku}
                        </p>
                      </td>

                      <td className="px-4 py-4 text-right text-sm text-gray-700">
                        {item.currentQuantity}
                      </td>

                      <td className="px-4 py-4 text-right text-sm text-gray-700">
                        {item.minimumThreshold}
                      </td>

                      <td className="px-4 py-4 text-right text-sm text-gray-700">
                        {item.averageDailyUsage.toFixed(2)}
                      </td>

                      <td className="px-4 py-4 text-right text-sm text-gray-700">
                        {item.predictedDemand}
                      </td>

                      <td className="px-4 py-4 text-right text-sm text-gray-700">
                        {item.projectedQuantity}
                      </td>

                      <td className="px-4 py-4 text-center">
                        <span
                          className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${getRiskClasses(
                            item.riskLevel
                          )}`}
                        >
                          {item.riskLevel}
                        </span>
                      </td>

                      <td className="px-4 py-4 text-right text-sm font-medium text-gray-900">
                        {item.recommendedOrderQuantity}
                      </td>

                      <td className="px-4 py-4 text-sm text-gray-600">
                        <p>{item.forecastMethod}</p>

                        <p className="text-xs text-gray-400">
                          {item.historicalCheckoutCount}{' '}
                          checkout events
                        </p>
                      </td>
                    </tr>
                  ))}

                  {forecast.items.length === 0 && (
                    <tr>
                      <td
                        colSpan={9}
                        className="px-4 py-10 text-center text-sm text-gray-500"
                      >
                        No inventory items were found.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}