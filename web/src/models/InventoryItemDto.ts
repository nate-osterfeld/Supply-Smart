export interface InventoryItemDto {
    id: number;
    name: string;
    sku: string;
    lotNumber: string;
    expirationDate: string;
    quantity: number;
    status: string;
    type: number;
}