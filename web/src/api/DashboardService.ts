import { BaseApi } from './BaseApi';
import type { AuditLogEntryDto, DashboardMetricsDto, ProcedureForecastSummaryDto } from '../models';

export interface IDashboardService {
    getMetrics(locationId?: number): Promise<DashboardMetricsDto>;
    getForecastSummary(): Promise<ProcedureForecastSummaryDto>;
    getRecentAuditLogs(): Promise<AuditLogEntryDto[]>;
}

export class DashboardService extends BaseApi implements IDashboardService {
    public constructor() { 
        super(true); // usesAuth = true for protected routes requiring JWT token
    }

    public getMetrics = async (locationId?: number): Promise<DashboardMetricsDto> => {
        const url = locationId ? `api/dashboard/metrics?locationId=${locationId}` : 'api/dashboard/metrics';
        const response = await this.get<DashboardMetricsDto>(url);
        return response.data;
    };

    public getForecastSummary = async (): Promise<ProcedureForecastSummaryDto> => {
        const response = await this.get<ProcedureForecastSummaryDto>('api/forecast/summary');
        return response.data;
    };

    public getRecentAuditLogs = async (): Promise<AuditLogEntryDto[]> => {
        const response = await this.get<AuditLogEntryDto[]>('api/audit-ledger/recent');
        return response.data;
    };
}