namespace SupplySmart.Integrations.Models.DTO;

public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public List<RoomDto> Rooms { get; set; } = new();
}
