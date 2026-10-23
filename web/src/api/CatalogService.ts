import { BaseApi } from './BaseApi';
import type { MasterCatalogItemDto, AuditLogEntryDto } from '../models';

export interface ICatalogService {
    getMasterCatalog(): Promise<MasterCatalogItemDto[]>;
    getAuditLogs(): Promise<AuditLogEntryDto[]>;
}

export class CatalogService extends BaseApi implements ICatalogService {
    public constructor() {
        super(true); // usesAuth = true for protected routes requiring JWT token
    }

    public getMasterCatalog = async (): Promise<MasterCatalogItemDto[]> => {
        const response = await this.get<MasterCatalogItemDto[]>('api/catalog');
        return response.data;
    };

    public getAuditLogs = async (): Promise<AuditLogEntryDto[]> => {
        const response = await this.get<AuditLogEntryDto[]>('api/catalog/audit-logs');
        return response.data;
    };
}