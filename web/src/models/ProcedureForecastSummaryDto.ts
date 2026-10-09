export interface ProcedureForecastSummaryDto {
    scheduledProceduresCount: number;
    requiredKitsCount: number;
    availableKitsCount: number;
    deficitMessage: string;
    isDeficitDetected: boolean;
}