export interface InventoryForecastDto {
  inventoryItemId: number;
  itemName: string;
  sku: string;
  currentQuantity: number;
  minimumThreshold: number;
  forecastDays: number;
  averageDailyUsage: number;
  predictedDemand: number;
  projectedQuantity: number;
  riskLevel: 'Low' | 'Medium' | 'Critical';
  recommendedOrderQuantity: number;
  historicalCheckoutCount: number;
  forecastMethod: string;
}