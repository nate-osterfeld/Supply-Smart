export interface InventoryItemDto {
    id: number;
    name: string;
    sku: string;
    lotNumber: string;
    expirationDate: string;
    quantity: number;
    minimumThreshold: number;
    unitCost: number;
    statuses: string[];
    category: string;
    type: number;
}