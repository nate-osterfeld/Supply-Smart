import { useQuery } from '@tanstack/react-query';
import { CatalogService } from '../api/CatalogService';

export function useCatalog() {
    const service = new CatalogService();

    const catalogQuery = useQuery({
        queryKey: ['master-catalog'],
        queryFn: () => service.getMasterCatalog(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    const auditLogsQuery = useQuery({
        queryKey: ['compliance-audit-logs'],
        queryFn: () => service.getAuditLogs(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    return {
        catalogItems: catalogQuery.data ?? [],
        isLoadingCatalog: catalogQuery.isLoading,
        auditLogs: auditLogsQuery.data ?? [],
        isLoadingAudits: auditLogsQuery.isLoading,
    };
}