using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using SupplySmart.Endpoints.Endpoints;

namespace SupplySmart.Endpoints.Configuration;

public static partial class EndpointMapper
{
    public static void MapSupplyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/supplies");

        group.MapGet("/", SuppliesEndpoint.GetSupplies)
            .WithName("GetSupplies")
            .WithDescription("Get all supply items");

        group.MapPost("/", SuppliesEndpoint.CreateSupply)
            .WithName("CreateSupply")
            .WithDescription("Create a new supply item");
    }
}