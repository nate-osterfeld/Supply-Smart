import { BaseApi } from './BaseApi';
import type {
    SubscriptionDetailsDto,
    SubscriptionCheckoutResponseDto,
    UpdateSubscriptionRequestDto,
    SubscriptionPortalResponseDto,
    VerifySessionRequestDto
} from '../models';

export interface ISubscriptionService {
    getSubscriptionDetails(): Promise<SubscriptionDetailsDto>;
    updateSubscriptionPlan(dto: UpdateSubscriptionRequestDto): Promise<SubscriptionCheckoutResponseDto>;
    createPortalSession(): Promise<SubscriptionPortalResponseDto>;
    verifySession(dto: VerifySessionRequestDto): Promise<SubscriptionDetailsDto>;
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

    public createPortalSession = async (): Promise<SubscriptionPortalResponseDto> => {
        const response = await this.post<SubscriptionPortalResponseDto>(
            'api/settings/subscription/portal',
            {}
        );
        return response.data;
    };

    public verifySession = async (dto: VerifySessionRequestDto): Promise<SubscriptionDetailsDto> => {
        const response = await this.post<SubscriptionDetailsDto>(
            'api/settings/subscription/verify-session',
            dto
        );
        return response.data;
    };
}