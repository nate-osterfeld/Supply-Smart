export interface SubscriptionDetailsDto {
    planName: string;
    status: string;
    billingInterval: string;
    renewalDateUtc: string;
    usedRoomsCount: number;
    maxRoomsQuota: number;
    usedItemsCount: number;
    maxItemsQuota: number;
    isStripeConfigured: boolean;
    stripeCustomerId?: string | null;
}