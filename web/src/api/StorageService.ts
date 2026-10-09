import { BaseApi } from './BaseApi';
import type { LocationDto, InventoryItemDto } from '../models';

export interface IStorageService {
    getLocations(): Promise<LocationDto[]>;
    getContainerItems(containerId?: number): Promise<InventoryItemDto[]>;
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
}