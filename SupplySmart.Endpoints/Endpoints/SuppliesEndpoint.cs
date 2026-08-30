using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models;

namespace SupplySmart.Endpoints.Endpoints;

public abstract class SuppliesEndpoint
{
    public static async Task<Results<Ok<List<SupplyItem>>, ProblemHttpResult>> GetSupplies(
        SupplySmartDbContext db,
        ILogger<SuppliesEndpoint> logger)
    {
        try
        {
            var supplies = await db.SupplyItems.ToListAsync();
            return TypedResults.Ok(supplies);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching supply items.");
            return TypedResults.Problem("An unexpected error occurred.");
        }
    }

    public static async Task<Results<Created<SupplyItem>, BadRequest<string>, ProblemHttpResult>> CreateSupply(
        SupplyItem newItem,
        SupplySmartDbContext db,
        ILogger<SuppliesEndpoint> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newItem.Name))
                return TypedResults.BadRequest("Item name is required.");

            db.SupplyItems.Add(newItem);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/api/supplies/{newItem.Id}", newItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating a supply item.");
            return TypedResults.Problem("An unexpected error occurred.");
        }
    }
}