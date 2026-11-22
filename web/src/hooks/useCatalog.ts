import { useQuery } from '@tanstack/react-query';
import { CatalogService } from '../api/CatalogService';

export function useCatalog() {
    const service = new CatalogService();

    const catalogQuery = useQuery({
        queryKey: ['master-catalog'],
        queryFn: () => service.getMasterCatalog(),
        retry: 1,
    });

    return {
        catalogItems: catalogQuery.data ?? [],
        isLoadingCatalog: catalogQuery.isLoading,
    };
}