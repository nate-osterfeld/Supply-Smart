export interface UpdateItemDto {
  name: string;
  sku: string;
  lotNumber: string;
  expirationDate?: string | null;
  quantity: number;
  minimumThreshold: number;
  unitCost: number;
  type: number; // 0 = Consumable, 1 = Reusable
}