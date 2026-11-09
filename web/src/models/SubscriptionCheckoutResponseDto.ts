export interface SubscriptionCheckoutResponseDto {
    updatedImmediately: boolean;
    checkoutUrl: string | null;
    message: string;
}