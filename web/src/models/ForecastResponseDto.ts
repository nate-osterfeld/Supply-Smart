import type { InventoryForecastDto } from './InventoryForecastDto';

export interface ForecastResponseDto {
  forecastDays: number;
  lookbackDays: number;
  totalPredictedDemand: number;
  itemsAtRisk: number;
  potentialStockouts: number;
  suggestedOrders: number;
  items: InventoryForecastDto[];
}