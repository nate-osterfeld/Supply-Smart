namespace SupplySmart.Integrations.Models.DTO
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ContainerDto> Containers { get; set; } = new();
    }
}