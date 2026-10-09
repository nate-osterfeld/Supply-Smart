export interface AuditLogEntryDto {
    id: number;
    timestamp: string;
    userEmail: string;
    actionType: string;
    itemDetails: string;
    locationContext: string;
}