import { useQuery } from '@tanstack/react-query';
import { CatalogService } from '../api/CatalogService';

export function useCatalog(canReadAudit: boolean) {
    const service = new CatalogService();

    const catalogQuery = useQuery({
        queryKey: ['master-catalog'],
        queryFn: () => service.getMasterCatalog(),
        retry: 1,
    });

    const auditLogsQuery = useQuery({
        queryKey: ['compliance-audit-logs'],
        enabled: canReadAudit,
        queryFn: () => service.getAuditLogs(),
        retry: 1,
    });

    return {
        catalogItems: catalogQuery.data ?? [],
        isLoadingCatalog: catalogQuery.isLoading,
        auditLogs: auditLogsQuery.data ?? [],
        isLoadingAudits: auditLogsQuery.isLoading,
    };
}