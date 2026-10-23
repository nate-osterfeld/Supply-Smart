export interface MasterCatalogItemDto {
    id: number;
    name: string;
    sku: string;
    category: string;
    quantity: number;
    location: string;
    expirationDate: string;
    status: string;
    lotNumber?: string;
    minimumThreshold?: number;
    unitCost?: number;
    type?: number;
}