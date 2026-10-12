import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { StorageService } from '../api/StorageService';
import type { CreateContainerDto, CreateItemDto } from '../models';

export function useStorage(containerId?: number) {
    const queryClient = useQueryClient();
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
        enabled: !!localStorage.getItem('supplysmart_token') && !!containerId,
    });

    // Mutations with automatic cache invalidation
    const createRoomMutation = useMutation({
        mutationFn: ({ locationId, name }: { locationId: number; name: string }) => 
            service.createRoom(locationId, name),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
        },
    });

    const createContainerMutation = useMutation({
        mutationFn: ({ roomId, dto }: { roomId: number; dto: CreateContainerDto }) => 
            service.createContainer(roomId, dto),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
        },
    });

    const addItemMutation = useMutation({
        mutationFn: ({ containerId, dto }: { containerId: number; dto: CreateItemDto }) => 
            service.addItem(containerId, dto),
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: ['container-items', variables.containerId] });
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] }); // updates item/capacity counts if displayed
        },
    });

    const moveItemMutation = useMutation({
        mutationFn: ({ itemId, targetContainerId, quantityToMove, minimumThreshold }: { 
            itemId: number; 
            targetContainerId: number; 
            quantityToMove?: number; 
            minimumThreshold?: number 
        }) => service.moveItem(itemId, targetContainerId, quantityToMove, minimumThreshold),
        onSuccess: () => {
            // Invalidate all container item caches to reflect the transfer
            queryClient.invalidateQueries({ queryKey: ['container-items'] });
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
        },
    });

    return {
        locations: locationsQuery.data ?? [],
        isLoadingLocations: locationsQuery.isLoading,
        items: itemsQuery.data ?? [],
        isLoadingItems: itemsQuery.isLoading,
        createRoom: createRoomMutation.mutateAsync,
        createContainer: createContainerMutation.mutateAsync,
        addItem: addItemMutation.mutateAsync,
        isCreating: createRoomMutation.isPending || createContainerMutation.isPending || addItemMutation.isPending,
        moveItem: moveItemMutation.mutateAsync,
        isMoving: moveItemMutation.isPending
    };
}