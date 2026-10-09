using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;
using System.Collections.Generic;

namespace SupplySmart.Endpoints.Configuration
{
    public static partial class EndpointMapper
    {
        public static void MapDashboardEndpoints(this WebApplication app)
        {
            var dashboardEndpoints = app.MapGroup("/api/dashboard")
                .WithTags("Dashboard")
                .RequireAuthorization();

            dashboardEndpoints.MapGet("/metrics", DashboardEndpoints.GetMetrics)
                .WithName("GetDashboardMetrics")
                .WithDescription("Fetches top-line KPI counts for inventory value, expirations, low stock, and pending restocks.")
                .Produces<DashboardMetricsDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            app.MapGet("/api/forecast/summary", DashboardEndpoints.GetForecastSummary)
                .WithName("GetForecastSummary")
                .WithDescription("Fetches the procedural demand comparison metrics and deficit warnings.")
                .RequireAuthorization()
                .Produces<ProcedureForecastSummaryDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            app.MapGet("/api/audit-ledger/recent", DashboardEndpoints.GetRecentAuditLogs)
                .WithName("GetRecentAuditLogs")
                .WithDescription("Fetches the last 5 immutable log entries for the activity feed snapshot.")
                .RequireAuthorization()
                .Produces<IEnumerable<AuditLogEntryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
    }
}