using System.ComponentModel.DataAnnotations;

namespace SupplySmart.Integrations.Models.Data;

public class Organization : IEntityBase
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;

    // --- SaaS Subscription Fields
    public string SubscriptionPlan { get; set; } = "Free";     // "Free", "Starter", "Professional", "Enterprise"
    public string SubscriptionStatus { get; set; } = "Active"; // "Active", "PastDue", "Canceled"
    public int MaxRoomsQuota { get; set; } = 2;                // 2 = Free, 5 = Starter, 15 = Pro, 9999 = Enterprise
    public int MaxItemsQuota { get; set; } = 50;               // 50 = Free, 250 = Starter, 1500 = Pro, 999999 = Enterprise
    public string BillingInterval { get; set; } = "Annual";    // "Monthly" or "Annual"
    public DateTime RenewalDateUtc { get; set; } = DateTime.UtcNow.AddYears(1);
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    // Required by IEntityBase tracking
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    public string LastUpdatedBy { get; set; } = string.Empty;
    public DateTime LastUpdatedDateUtc { get; set; } = DateTime.UtcNow;


    // Navigation collections for multi-tenant scoping
    public List<User> Users { get; set; } = new List<User>();
    public List<Location> Locations { get; set; } = new List<Location>();
    public List<Room> Rooms { get; set; } = new List<Room>();
    public List<Container> Containers { get; set; } = new List<Container>();
    public List<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public List<NotificationTemplate> NotificationTemplates { get; set; } = new List<NotificationTemplate>();
}
