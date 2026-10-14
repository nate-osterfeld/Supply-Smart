import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { StorageService } from '../api/StorageService';
import type { CreateContainerDto, CreateItemDto, UpdateItemDto } from '../models';

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

    const updateRoomMutation = useMutation({
        mutationFn: ({ roomId, name }: { roomId: number; name: string }) => 
            service.updateRoom(roomId, name),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
        },
    });

    const deleteRoomMutation = useMutation({
        mutationFn: (roomId: number) => service.deleteRoom(roomId),
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

    const updateContainerMutation = useMutation({
        mutationFn: ({ containerId, dto }: { containerId: number; dto: { name: string; kind: string } }) => 
            service.updateContainer(containerId, dto),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
        },
    });

    const deleteContainerMutation = useMutation({
        mutationFn: (containerId: number) => service.deleteContainer(containerId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
            queryClient.invalidateQueries({ queryKey: ['container-items'] });
        },
    });

    const addItemMutation = useMutation({
        mutationFn: ({ containerId, dto }: { containerId: number; dto: CreateItemDto }) => 
            service.addItem(containerId, dto),
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: ['container-items', variables.containerId] });
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] }); 
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
            queryClient.invalidateQueries({ queryKey: ['container-items'] });
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
        },
    });

    const updateItemMutation = useMutation({
        mutationFn: ({ itemId, dto }: { itemId: number; dto: UpdateItemDto }) => 
            service.updateItem(itemId, dto),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['container-items'] });
            queryClient.invalidateQueries({ queryKey: ['locations-hierarchy'] });
        },
    });

    const deleteItemMutation = useMutation({
        mutationFn: (itemId: number) => service.deleteItem(itemId),
        onSuccess: () => {
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
        updateRoom: updateRoomMutation.mutateAsync,
        deleteRoom: deleteRoomMutation.mutateAsync,
        createContainer: createContainerMutation.mutateAsync,
        updateContainer: updateContainerMutation.mutateAsync,
        deleteContainer: deleteContainerMutation.mutateAsync,
        addItem: addItemMutation.mutateAsync,
        updateItem: updateItemMutation.mutateAsync,
        deleteItem: deleteItemMutation.mutateAsync,
        isCreating: createRoomMutation.isPending || createContainerMutation.isPending || addItemMutation.isPending,
        moveItem: moveItemMutation.mutateAsync,
        isMoving: moveItemMutation.isPending,
        isUpdating: updateItemMutation.isPending,
        isDeleting: deleteItemMutation.isPending || deleteRoomMutation.isPending || deleteContainerMutation.isPending
    };
}