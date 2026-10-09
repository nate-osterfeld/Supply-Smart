public class ProcedureForecastSummaryDto
{
    public int ScheduledProceduresCount { get; set; }
    public int RequiredKitsCount { get; set; }
    public int AvailableKitsCount { get; set; }
    public string DeficitMessage { get; set; } = string.Empty;
    public bool IsDeficitDetected { get; set; }
}