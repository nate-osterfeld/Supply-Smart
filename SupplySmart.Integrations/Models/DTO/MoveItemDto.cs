namespace SupplySmart.Integrations.Models.DTO;

public class MoveItemDto
{
    public int TargetContainerId { get; set; }
    public int? Quantity { get; set; } // Optional: if quantity not included in request, move the full item quantity
    public int? MinimumThreshold { get; set; } // Optional: Sets or overrides the par level in the destination bin
}
