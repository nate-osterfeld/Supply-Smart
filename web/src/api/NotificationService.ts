import { BaseApi } from './BaseApi';
import type { NotificationTemplateDto, UpdateNotificationTemplateRequestDto } from '../models';

export interface INotificationService {
    getTemplates(): Promise<NotificationTemplateDto[]>;
    updateTemplate(dto: UpdateNotificationTemplateRequestDto): Promise<NotificationTemplateDto>;
    sendTestAlert(dto: UpdateNotificationTemplateRequestDto): Promise<{ recipientCount: number }>;
}

export class NotificationService extends BaseApi {
    public constructor() {
        super(true);
    }

    public getTemplates = async (): Promise<NotificationTemplateDto[]> => {
        const response = await this.get<NotificationTemplateDto[]>('api/settings/notifications');
        return response.data;
    };

    public updateTemplate = async (dto: UpdateNotificationTemplateRequestDto): Promise<NotificationTemplateDto> => {
        const response = await this.put<NotificationTemplateDto>('api/settings/notifications', dto);
        return response.data;
    };

    public sendTestAlert = async (dto: UpdateNotificationTemplateRequestDto): Promise<{ recipientCount: number }> => {
        const response = await this.post<{ recipientCount: number }>('api/settings/notifications/test', dto);
        return response.data;
    };
}