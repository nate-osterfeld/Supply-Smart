import { BaseApi } from './BaseApi';
import type { SubscriptionDetailsDto, SubscriptionCheckoutResponseDto, UpdateSubscriptionRequestDto } from '../models';

export interface ISubscriptionService {
    getSubscriptionDetails(): Promise<SubscriptionDetailsDto>;
    updateSubscriptionPlan(dto: UpdateSubscriptionRequestDto): Promise<SubscriptionCheckoutResponseDto>;
}

export class SubscriptionService extends BaseApi implements ISubscriptionService {
    public constructor() {
        super(true);
    }

    public getSubscriptionDetails = async (): Promise<SubscriptionDetailsDto> => {
        const response = await this.get<SubscriptionDetailsDto>('api/settings/subscription');
        return response.data;
    };

    public updateSubscriptionPlan = async (dto: UpdateSubscriptionRequestDto): Promise<SubscriptionCheckoutResponseDto> => {
        const response = await this.post<SubscriptionCheckoutResponseDto>(
            'api/settings/subscription/checkout',
            dto
        );
        return response.data;
    };
}