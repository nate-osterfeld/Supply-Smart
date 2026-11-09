using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDetailsDto> GetSubscriptionDetailsAsync(int organizationId);
        Task<SubscriptionCheckoutResponseDto> UpdateSubscriptionTierAsync(int organizationId, string targetPlan, string userEmail);
        Task<SubscriptionPortalResponseDto> CreateCustomerPortalSessionAsync(int organizationId);
        Task<SubscriptionDetailsDto> VerifyAndSyncSessionAsync(int organizationId, string sessionId);
        Task HandleStripeWebhookAsync(string jsonPayload, string stripeSignatureHeader);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly SupplySmartDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _webhookSecret;
        private readonly string _frontendUrl;

        public SubscriptionService(SupplySmartDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            _secretKey = _configuration["StripeSettings:SecretKey"] ?? string.Empty;
            _webhookSecret = _configuration["StripeSettings:WebhookSecret"] ?? string.Empty;
            _frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";

            if (!string.IsNullOrWhiteSpace(_secretKey))
            {
                StripeConfiguration.ApiKey = _secretKey;
            }
        }

        public async Task<SubscriptionDetailsDto> GetSubscriptionDetailsAsync(int organizationId)
        {
            var org = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null)
            {
                throw new InvalidOperationException("Organization not found.");
            }

            var usedRoomsCount = await _context.Rooms
                .CountAsync(r => r.OrganizationId == organizationId);

            var usedItemsCount = await _context.InventoryItems
                .CountAsync(i => i.OrganizationId == organizationId);

            var isStripeConfigured = !string.IsNullOrWhiteSpace(_secretKey);

            return new SubscriptionDetailsDto
            {
                PlanName = string.IsNullOrWhiteSpace(org.SubscriptionPlan) ? "Free" : org.SubscriptionPlan,
                Status = string.IsNullOrWhiteSpace(org.SubscriptionStatus) ? "Active" : org.SubscriptionStatus,
                BillingInterval = string.IsNullOrWhiteSpace(org.BillingInterval) ? "Annual" : org.BillingInterval,
                RenewalDateUtc = org.RenewalDateUtc == default ? DateTime.UtcNow.AddYears(1) : org.RenewalDateUtc,
                UsedRoomsCount = usedRoomsCount,
                MaxRoomsQuota = org.MaxRoomsQuota > 0 ? org.MaxRoomsQuota : 2,
                UsedItemsCount = usedItemsCount,
                MaxItemsQuota = org.MaxItemsQuota > 0 ? org.MaxItemsQuota : 50,
                IsStripeConfigured = isStripeConfigured,
                StripeCustomerId = org.StripeCustomerId
            };
        }

        public async Task<SubscriptionCheckoutResponseDto> UpdateSubscriptionTierAsync(int organizationId, string targetPlan, string userEmail)
        {
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null)
            {
                throw new InvalidOperationException("Organization not found.");
            }

            var normalizedPlan = targetPlan?.Trim() switch
            {
                var p when string.Equals(p, "Free", StringComparison.OrdinalIgnoreCase) => "Free",
                var p when string.Equals(p, "Starter", StringComparison.OrdinalIgnoreCase) => "Starter",
                var p when string.Equals(p, "Professional", StringComparison.OrdinalIgnoreCase) => "Professional",
                var p when string.Equals(p, "Enterprise", StringComparison.OrdinalIgnoreCase) => "Enterprise",
                _ => throw new ArgumentException("Invalid subscription plan selected. Choose Free, Starter, Professional, or Enterprise.")
            };

            var (targetRoomsQuota, targetItemsQuota) = normalizedPlan switch
            {
                "Free" => (2, 50),
                "Starter" => (5, 250),
                "Professional" => (15, 1500),
                "Enterprise" => (9999, 999999),
                _ => (2, 50)
            };

            var currentRoomsCount = await _context.Rooms
                .CountAsync(r => r.OrganizationId == organizationId);

            var currentItemsCount = await _context.InventoryItems
                .CountAsync(i => i.OrganizationId == organizationId);

            if (currentRoomsCount > targetRoomsQuota)
            {
                throw new InvalidOperationException(
                    $"Cannot switch to the {normalizedPlan} Plan ({targetRoomsQuota} rooms max) because your workspace currently has {currentRoomsCount} active supply rooms.");
            }

            if (currentItemsCount > targetItemsQuota)
            {
                throw new InvalidOperationException(
                    $"Cannot switch to the {normalizedPlan} Plan ({targetItemsQuota} items max) because your workspace currently tracks {currentItemsCount} inventory items.");
            }

            string priceId = normalizedPlan switch
            {
                "Starter" => _configuration["StripeSettings:StarterPriceId"] ?? string.Empty,
                "Professional" => _configuration["StripeSettings:ProfessionalPriceId"] ?? string.Empty,
                "Enterprise" => _configuration["StripeSettings:EnterprisePriceId"] ?? string.Empty,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(_secretKey) || normalizedPlan == "Free" || string.IsNullOrWhiteSpace(priceId))
            {
                ApplyLocalPlanUpdate(org, normalizedPlan, targetRoomsQuota, targetItemsQuota, userEmail);
                await _context.SaveChangesAsync();

                return new SubscriptionCheckoutResponseDto
                {
                    UpdatedImmediately = true,
                    CheckoutUrl = null,
                    Message = $"Successfully updated workspace subscription to the {normalizedPlan} Plan."
                };
            }

            if (string.IsNullOrEmpty(org.StripeCustomerId))
            {
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Name = org.Name,
                    Email = userEmail,
                    Metadata = new Dictionary<string, string> { { "OrganizationId", organizationId.ToString() } }
                });
                org.StripeCustomerId = customer.Id;
                await _context.SaveChangesAsync();
            }

            var options = new SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Price = priceId, Quantity = 1 }
                },
                Mode = "subscription",
                SuccessUrl = $"{_frontendUrl}/dashboard?tab=settings&session_id={{CHECKOUT_SESSION_ID}}&billing_success=true&plan={normalizedPlan}",
                CancelUrl = $"{_frontendUrl}/dashboard?tab=settings&billing_canceled=true",
                Metadata = new Dictionary<string, string>
                {
                    { "OrganizationId", organizationId.ToString() },
                    { "TargetPlan", normalizedPlan }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return new SubscriptionCheckoutResponseDto
            {
                UpdatedImmediately = false,
                CheckoutUrl = session.Url,
                Message = "Redirecting to Stripe Checkout..."
            };
        }

        public async Task<SubscriptionPortalResponseDto> CreateCustomerPortalSessionAsync(int organizationId)
        {
            var org = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId);
            if (org == null || string.IsNullOrEmpty(org.StripeCustomerId))
            {
                throw new InvalidOperationException("No active billing profile or customer ID found for this organization.");
            }

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                ReturnUrl = $"{_frontendUrl}/dashboard?tab=settings"
            };

            var service = new Stripe.BillingPortal.SessionService();
            var portalSession = await service.CreateAsync(options);

            return new SubscriptionPortalResponseDto
            {
                PortalUrl = portalSession.Url
            };
        }

        public async Task<SubscriptionDetailsDto> VerifyAndSyncSessionAsync(int organizationId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(_secretKey) || string.IsNullOrWhiteSpace(sessionId))
            {
                return await GetSubscriptionDetailsAsync(organizationId);
            }

            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);

            if (session != null && session.PaymentStatus == "paid")
            {
                if (session.Metadata.TryGetValue("TargetPlan", out var targetPlan) &&
                    session.Metadata.TryGetValue("OrganizationId", out var orgIdStr) &&
                    int.TryParse(orgIdStr, out var orgId) && orgId == organizationId)
                {
                    var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
                    if (org != null)
                    {
                        org.StripeSubscriptionId = session.SubscriptionId;

                        var (rooms, items) = targetPlan switch
                        {
                            "Starter" => (5, 250),
                            "Professional" => (15, 1500),
                            "Enterprise" => (9999, 999999),
                            _ => (2, 50)
                        };

                        ApplyLocalPlanUpdate(org, targetPlan, rooms, items, "Stripe Webhook/Verify");
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return await GetSubscriptionDetailsAsync(organizationId);
        }

        public async Task HandleStripeWebhookAsync(string jsonPayload, string stripeSignatureHeader)
        {
            if (string.IsNullOrWhiteSpace(_webhookSecret)) return;

            var stripeEvent = EventUtility.ConstructEvent(jsonPayload, stripeSignatureHeader, _webhookSecret);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && session.Metadata.TryGetValue("OrganizationId", out var orgIdStr) &&
                    int.TryParse(orgIdStr, out var orgId) &&
                    session.Metadata.TryGetValue("TargetPlan", out var targetPlan))
                {
                    var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
                    if (org != null)
                    {
                        org.StripeSubscriptionId = session.SubscriptionId;
                        var (rooms, items) = targetPlan switch
                        {
                            "Starter" => (5, 250),
                            "Professional" => (15, 1500),
                            "Enterprise" => (9999, 999999),
                            _ => (2, 50)
                        };

                        ApplyLocalPlanUpdate(org, targetPlan, rooms, items, "Stripe Webhook");
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else if (stripeEvent.Type == "customer.subscription.updated" || stripeEvent.Type == "customer.subscription.deleted")
            {
                var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                if (subscription != null)
                {
                    var org = await _context.Organizations.FirstOrDefaultAsync(o => o.StripeCustomerId == subscription.CustomerId);
                    if (org != null)
                    {
                        // If subscription was deleted or explicitly canceled
                        if (stripeEvent.Type == "customer.subscription.deleted" || subscription.Status == "canceled")
                        {
                            ApplyLocalPlanUpdate(org, "Free", 2, 50, "Stripe Webhook (Canceled)");
                        }
                        else if (subscription.Items?.Data != null && subscription.Items.Data.Count > 0)
                        {
                            var priceId = subscription.Items.Data[0].Price?.Id;
                            string planName = "Free";
                            int rooms = 2;
                            int items = 50;

                            if (priceId == _configuration["StripeSettings:StarterPriceId"])
                            {
                                planName = "Starter";
                                rooms = 5;
                                items = 250;
                            }
                            else if (priceId == _configuration["StripeSettings:ProfessionalPriceId"])
                            {
                                planName = "Professional";
                                rooms = 15;
                                items = 1500;
                            }
                            else if (priceId == _configuration["StripeSettings:EnterprisePriceId"])
                            {
                                planName = "Enterprise";
                                rooms = 9999;
                                items = 999999;
                            }

                            ApplyLocalPlanUpdate(org, planName, rooms, items, "Stripe Webhook (Updated)");
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private void ApplyLocalPlanUpdate(Organization org, string plan, int roomsQuota, int itemsQuota, string updatedBy)
        {
            org.SubscriptionPlan = plan;
            org.SubscriptionStatus = "Active";
            org.MaxRoomsQuota = roomsQuota;
            org.MaxItemsQuota = itemsQuota;
            if (org.RenewalDateUtc == default || org.RenewalDateUtc < DateTime.UtcNow)
            {
                org.RenewalDateUtc = DateTime.UtcNow.AddYears(1);
            }
            org.LastUpdatedDateUtc = DateTime.UtcNow;
            org.LastUpdatedBy = updatedBy;

            var roomsDisplay = roomsQuota == 9999 ? "Unlimited" : roomsQuota.ToString();
            var itemsDisplay = itemsQuota == 999999 ? "Unlimited" : itemsQuota.ToString();

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = org.Id,
                Timestamp = DateTime.UtcNow,
                UserEmail = updatedBy,
                ActionType = "Subscription Update",
                ItemDetails = $"Updated SaaS plan to {plan} Plan ({roomsDisplay} rooms, {itemsDisplay} items quota)",
                LocationName = "Workspace Settings"
            });
        }
    }
}