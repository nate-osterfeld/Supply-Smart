import { useQuery } from '@tanstack/react-query';
import { DashboardService } from '../api/DashboardService';

export function useDashboard(locationId?: number) {
    const service = new DashboardService();

    const metricsQuery = useQuery({
        queryKey: ['dashboard-metrics', locationId],
        queryFn: () => service.getMetrics(locationId),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    const forecastQuery = useQuery({
        queryKey: ['forecast-summary'],
        queryFn: () => service.getForecastSummary(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    const auditLogsQuery = useQuery({
        queryKey: ['recent-audit-logs'],
        queryFn: () => service.getRecentAuditLogs(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    return {
        metrics: metricsQuery.data,
        isLoadingMetrics: metricsQuery.isLoading,
        forecast: forecastQuery.data,
        isLoadingForecast: forecastQuery.isLoading,
        auditLogs: auditLogsQuery.data ?? [],
        isLoadingAuditLogs: auditLogsQuery.isLoading,
    };
}