using System;
using System.Collections.Generic;
using System.Text;

namespace SupplySmart.Integrations.Models.DTO;

public class SubscriptionCheckoutResponseDto
{
    public bool UpdatedImmediately { get; set; }
    public string? CheckoutUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}
