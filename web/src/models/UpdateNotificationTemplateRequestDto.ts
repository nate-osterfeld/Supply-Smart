export interface UpdateNotificationTemplateRequestDto {
    triggerType: string;
    templateMarkdown: string;
    additionalRecipients: string[];
}