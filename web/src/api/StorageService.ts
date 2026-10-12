import { BaseApi } from './BaseApi';
import type { LocationDto, InventoryItemDto, CreateContainerDto, CreateItemDto, ContainerDto, RoomDto } from '../models';

export interface IStorageService {
    getLocations(): Promise<LocationDto[]>;
    getContainerItems(containerId?: number): Promise<InventoryItemDto[]>;
    createRoom(locationId: number, name: string): Promise<RoomDto>;
    createContainer(roomId: number, dto: CreateContainerDto): Promise<ContainerDto>;
    addItem(containerId: number, dto: CreateItemDto): Promise<InventoryItemDto>;
}

export class StorageService extends BaseApi implements IStorageService {
    public constructor() {
        super(true); // usesAuth = true for protected routes requiring JWT token
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

    public createRoom = async (locationId: number, name: string): Promise<RoomDto> => {
        const response = await this.post<RoomDto>(`api/locations/rooms/${locationId}`, { name });
        return response.data;
    };

    public createContainer = async (roomId: number, dto: CreateContainerDto): Promise<ContainerDto> => {
        const response = await this.post<ContainerDto>(`api/locations/rooms/${roomId}/containers`, dto);
        return response.data;
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
}