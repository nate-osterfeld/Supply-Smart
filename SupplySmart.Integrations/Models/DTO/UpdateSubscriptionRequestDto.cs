using System;
using System.Collections.Generic;
using System.Text;

namespace SupplySmart.Integrations.Models.DTO;

public class UpdateSubscriptionRequestDto
{
    public string TargetPlan { get; set; } = string.Empty; // "Free", "Starter", "Professional", or "Enterprise"
}
