using System;
using System.Collections.Generic;
using System.Text;

namespace SupplySmart.Integrations.Models.DTO;

public class SubscriptionDetailsDto
{
    public string PlanName { get; set; } = "Free";
    public string Status { get; set; } = "Active";
    public string BillingInterval { get; set; } = "Annual";
    public DateTime RenewalDateUtc { get; set; }
    public int UsedRoomsCount { get; set; }
    public int MaxRoomsQuota { get; set; }
    public int UsedItemsCount { get; set; }
    public int MaxItemsQuota { get; set; }
    public bool IsStripeConfigured { get; set; }
}
