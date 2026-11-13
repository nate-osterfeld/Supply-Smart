export interface NotificationTemplateDto {
    id: number;
    triggerType: string;
    templateMarkdown: string;
    additionalRecipients: string[];
    lastUpdatedBy: string;
    lastUpdatedDateUtc: string;
}