export interface DashboardMetricsDto {
    totalInventoryValue: number;
    expiringSoonCount: number;
    activeLowStockAlertsCount: number;
    pendingRestocksCount: number;
    safeStockPercentage: number;
    expiringSoonPercentage: number;
    expiredPercentage: number;
}