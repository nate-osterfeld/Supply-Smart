export interface CreateItemDto {
  name: string;
  sku: string;
  lotNumber: string;
  expirationDate?: string | null;
  quantity: number;
  globalThreshold: number;
  localThreshold?: number | null;
  unitCost: number;
  type: number; // 0 = Consumable, 1 = Reusable
}