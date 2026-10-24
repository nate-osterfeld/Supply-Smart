import { BaseApi } from './BaseApi';
import type { LocationDto, InventoryItemDto, CreateContainerDto, CreateItemDto, ContainerDto, RoomDto, UpdateItemDto, CreateLocationDto } from '../models';

export interface IStorageService {
    getLocations(): Promise<LocationDto[]>;
    getContainerItems(containerId?: number): Promise<InventoryItemDto[]>;
    createLocation(dto: CreateLocationDto): Promise<LocationDto>;
    updateLocation(locationId: number, dto: CreateLocationDto): Promise<void>;
    deleteLocation(locationId: number): Promise<void>;
    createRoom(locationId: number, name: string): Promise<RoomDto>;
    updateRoom(roomId: number, name: string): Promise<void>;
    deleteRoom(roomId: number): Promise<void>;
    createContainer(roomId: number, dto: CreateContainerDto): Promise<ContainerDto>;
    updateContainer(containerId: number, dto: CreateContainerDto): Promise<void>; // Reusing CreateContainerDto
    deleteContainer(containerId: number): Promise<void>;
    addItem(containerId: number, dto: CreateItemDto): Promise<InventoryItemDto>;
    moveItem(itemId: number, targetContainerId: number, quantityToMove?: number, minimumThreshold?: number): Promise<boolean>;
    updateItem(itemId: number, dto: UpdateItemDto): Promise<void>;
    deleteItem(itemId: number): Promise<void>;
}

export class StorageService extends BaseApi implements IStorageService {
    public constructor() {
        super(true);
    }

    public getLocations = async (): Promise<LocationDto[]> => {
        const response = await this.get<LocationDto[]>('api/locations');
        return response.data;
    };

    public getContainerItems = async (containerId?: number): Promise<InventoryItemDto[]> => {
        const url = containerId ? `api/locations/items?containerId=${containerId}` : 'api/locations/items';
        const response = await this.get<InventoryItemDto[]>(url);
        return response.data;
    };

    public createLocation = async (dto: CreateLocationDto): Promise<LocationDto> => {
        const response = await this.post<LocationDto>('api/locations', dto);
        return response.data;
    };

    public updateLocation = async (locationId: number, dto: CreateLocationDto): Promise<void> => {
        await this.put<void>(`api/locations/${locationId}`, dto);
    };

    public deleteLocation = async (locationId: number): Promise<void> => {
        await this.delete<void>(`api/locations/${locationId}`);
    };

    public createRoom = async (locationId: number, name: string): Promise<RoomDto> => {
        const response = await this.post<RoomDto>(`api/locations/rooms/${locationId}`, { name });
        return response.data;
    };

    public updateRoom = async (roomId: number, name: string): Promise<void> => {
        await this.put<void>(`api/locations/rooms/${roomId}`, { name });
    };

    public deleteRoom = async (roomId: number): Promise<void> => {
        await this.delete<void>(`api/locations/rooms/${roomId}`);
    };

    public createContainer = async (roomId: number, dto: CreateContainerDto): Promise<ContainerDto> => {
        const response = await this.post<ContainerDto>(`api/locations/rooms/${roomId}/containers`, dto);
        return response.data;
    };

    public updateContainer = async (containerId: number, dto: CreateContainerDto): Promise<void> => {
        await this.put<void>(`api/locations/containers/${containerId}`, dto);
    };

    public deleteContainer = async (containerId: number): Promise<void> => {
        await this.delete<void>(`api/locations/containers/${containerId}`);
    };

    public addItem = async (containerId: number, dto: CreateItemDto): Promise<InventoryItemDto> => {
        const response = await this.post<InventoryItemDto>(`api/locations/containers/${containerId}/items`, dto);
        return response.data;
    };

    public moveItem = async (itemId: number, targetContainerId: number, quantityToMove?: number, minimumThreshold?: number): Promise<boolean> => {
        const response = await this.post<boolean>(`api/locations/items/${itemId}/move`, {
            targetContainerId,
            quantity: quantityToMove,
            minimumThreshold
        });
        return response.data;
    };

    public updateItem = async (itemId: number, dto: UpdateItemDto): Promise<void> => {
        await this.put<void>(`api/locations/items/${itemId}`, dto);
    };

    public deleteItem = async (itemId: number): Promise<void> => {
        await this.delete<void>(`api/locations/items/${itemId}`);
    };
}