using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Configuration
{
    public static partial class EndpointMapper
    {
        public static void MapCatalogEndpoints(this WebApplication app)
        {
            var catalogEndpoints = app.MapGroup("/api/catalog")
                .WithTags("Master Catalog & Audit Governance")
                .RequireAuthorization();

            catalogEndpoints.MapGet("/", CatalogEndpoints.GetMasterCatalog)
                .WithName("GetMasterCatalog")
                .Produces<IEnumerable<MasterCatalogItemDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);

            catalogEndpoints.MapGet("/audit-logs", CatalogEndpoints.GetAuditLogs)
                .WithName("GetAuditLogs")
                .Produces<IEnumerable<AuditLogEntryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }
}