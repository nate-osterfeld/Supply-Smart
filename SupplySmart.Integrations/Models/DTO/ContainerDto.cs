namespace SupplySmart.Integrations.Models.DTO
{
    public class ContainerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty; // e.g., Cabinet, Shelf, Bin
    }
}