export interface InventoryItemDto {
    id: number;
    name: string;
    sku: string;
    lotNumber: string;
    expirationDate: string;
    quantity: number;
    statuses: string[];
    type: number;
}