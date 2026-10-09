import { useQuery } from '@tanstack/react-query';
import { StorageService } from '../api/StorageService';

export function useStorage(containerId?: number) {
    const service = new StorageService();

    const locationsQuery = useQuery({
        queryKey: ['locations-hierarchy'],
        queryFn: () => service.getLocations(),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    const itemsQuery = useQuery({
        queryKey: ['container-items', containerId],
        queryFn: () => service.getContainerItems(containerId),
        retry: 1,
        enabled: !!localStorage.getItem('supplysmart_token'),
    });

    return {
        locations: locationsQuery.data ?? [],
        isLoadingLocations: locationsQuery.isLoading,
        items: itemsQuery.data ?? [],
        isLoadingItems: itemsQuery.isLoading,
    };
}