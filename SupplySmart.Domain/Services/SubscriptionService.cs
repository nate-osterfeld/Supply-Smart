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
        private readonly IStripeClient? _stripe;
        private readonly string _webhookSecret;
        private readonly string _frontendUrl;

        public SubscriptionService(SupplySmartDbContext context, IConfiguration configuration, IStripeClient? stripeClient = null)
        {
            _context = context;
            _configuration = configuration;

            var secretKey = _configuration["StripeSettings:SecretKey"];
            _stripe = stripeClient ?? (string.IsNullOrWhiteSpace(secretKey) ? null : new StripeClient(secretKey));
            _webhookSecret = _configuration["StripeSettings:WebhookSecret"] ?? string.Empty;
            _frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
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
                IsStripeConfigured = _stripe is not null,
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

            var (targetRoomsQuota, targetItemsQuota) = QuotasFor(normalizedPlan);

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

            var priceId = _configuration[$"StripeSettings:{normalizedPlan}PriceId"];

            if (normalizedPlan != "Free" &&
                (_stripe is null || string.IsNullOrWhiteSpace(priceId)))
            {
                throw new InvalidOperationException("Paid subscriptions are unavailable until Stripe billing is configured.");
            }

            if (normalizedPlan == "Free")
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
                var customerService = new CustomerService(_stripe);
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

            if (!string.IsNullOrEmpty(org.StripeSubscriptionId))
                options.Metadata["PreviousSubscriptionId"] = org.StripeSubscriptionId;

            var service = new SessionService(_stripe);
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
            if (_stripe is null || org == null || string.IsNullOrEmpty(org.StripeCustomerId))
            {
                throw new InvalidOperationException("No active billing profile or customer ID found for this organization.");
            }

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = org.StripeCustomerId,
                ReturnUrl = $"{_frontendUrl}/dashboard?tab=settings"
            };

            var service = new Stripe.BillingPortal.SessionService(_stripe);
            var portalSession = await service.CreateAsync(options);

            return new SubscriptionPortalResponseDto
            {
                PortalUrl = portalSession.Url
            };
        }

        public async Task<SubscriptionDetailsDto> VerifyAndSyncSessionAsync(int organizationId, string sessionId)
        {
            if (_stripe is not null && !string.IsNullOrWhiteSpace(sessionId))
                await SyncPaidCheckoutAsync(organizationId, sessionId, "Stripe Checkout Verification");
            return await GetSubscriptionDetailsAsync(organizationId);
        }

        public async Task HandleStripeWebhookAsync(string jsonPayload, string stripeSignatureHeader)
        {
            if (string.IsNullOrWhiteSpace(_webhookSecret)) return;
            var stripeEvent = EventUtility.ConstructEvent(jsonPayload, stripeSignatureHeader, _webhookSecret);

            if (stripeEvent.Type is "checkout.session.completed" or "checkout.session.async_payment_succeeded")
            {
                if (stripeEvent.Data.Object is Session { PaymentStatus: "paid" } session &&
                    session.Metadata?.TryGetValue("OrganizationId", out var orgId) == true &&
                    int.TryParse(orgId, out var organizationId) && !string.IsNullOrEmpty(session.Id))
                    await SyncPaidCheckoutAsync(organizationId, session.Id, "Stripe Webhook");
            }
            else if ((stripeEvent.Type is "customer.subscription.updated" or "customer.subscription.deleted") &&
                     stripeEvent.Data.Object is Stripe.Subscription subscription)
            {
                await SyncSubscriptionEventAsync(subscription, stripeEvent.Type == "customer.subscription.deleted");
            }
        }

        private async Task SyncPaidCheckoutAsync(int organizationId, string sessionId, string actor)
        {
            if (_stripe is null) throw new InvalidOperationException("Stripe billing is unavailable.");
            var checkout = await new SessionService(_stripe).GetAsync(sessionId,
                new SessionGetOptions { Expand = ["subscription"] });
            if (checkout.Mode != "subscription" || checkout.Status != "complete" || checkout.PaymentStatus != "paid")
                return;
            if (checkout.Metadata?.TryGetValue("OrganizationId", out var clinicId) != true ||
                !int.TryParse(clinicId, out var clinic) || clinic != organizationId)
                return;
            if (checkout.Subscription is not { Status: "active" or "trialing" } subscription ||
                string.IsNullOrEmpty(subscription.Id) || string.IsNullOrEmpty(checkout.CustomerId) ||
                subscription.CustomerId != checkout.CustomerId)
                return;

            var plan = PaidPlanFor(subscription);
            if (plan is null) return;
            var org = await _context.Organizations.AsNoTracking().SingleOrDefaultAsync(o => o.Id == organizationId);
            if (org is null || org.StripeCustomerId != checkout.CustomerId) return;
            if (org.StripeSubscriptionId == subscription.Id)
            {
                // A historical receipt must not undo an explicit switch to the Free plan.
                if (org.SubscriptionPlan != "Free" || org.SubscriptionStatus == "Unpaid")
                    await SaveStripePlanAsync(org, subscription.Id, plan, "Active", actor);
                return;
            }

            if (org.StripeSubscriptionId is { } currentId &&
                checkout.Metadata.GetValueOrDefault("PreviousSubscriptionId") != currentId)
            {
                // Older checkouts lack predecessor metadata. Only a newer subscription may replace the linked one.
                var linked = await new Stripe.SubscriptionService(_stripe).GetAsync(currentId);
                if (linked.CustomerId != checkout.CustomerId || subscription.Created <= linked.Created) return;
            }

            await SaveStripePlanAsync(org, subscription.Id, plan, "Active", actor);
        }

        private async Task SyncSubscriptionEventAsync(Stripe.Subscription signal, bool deleted)
        {
            if (string.IsNullOrWhiteSpace(signal.CustomerId) || string.IsNullOrWhiteSpace(signal.Id)) return;
            var org = await _context.Organizations.AsNoTracking().SingleOrDefaultAsync(o =>
                o.StripeCustomerId == signal.CustomerId && o.StripeSubscriptionId == signal.Id);
            if (org is null || org.StripeCustomerId != signal.CustomerId || org.StripeSubscriptionId != signal.Id) return;

            string plan = "Free";
            string status = "Canceled";
            if (!deleted)
            {
                if (_stripe is null) throw new InvalidOperationException("Stripe billing is unavailable.");
                // Stripe may deliver updates out of order. Read the subscription as it exists now.
                var current = await new Stripe.SubscriptionService(_stripe).GetAsync(signal.Id);
                if (current.Id != signal.Id || current.CustomerId != signal.CustomerId) return;
                if (current.Status is "active" or "trialing")
                {
                    plan = PaidPlanFor(current) ?? "Free";
                    status = "Active";
                }
                else if (current.Status == "unpaid")
                    status = "Unpaid";
                else if (current.Status is not ("canceled" or "incomplete_expired"))
                    return;
            }

            await SaveStripePlanAsync(org, signal.Id, plan, status, "Stripe Webhook");
        }

        private async Task SaveStripePlanAsync(Organization observed, string subscriptionId, string plan, string status, string actor)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var org = await _context.Organizations.SingleOrDefaultAsync(o => o.Id == observed.Id);
            if (org is null || org.StripeCustomerId != observed.StripeCustomerId) return;
            var (rooms, items) = QuotasFor(plan);
            if (org.StripeSubscriptionId == subscriptionId && org.SubscriptionPlan == plan && org.SubscriptionStatus == status &&
                org.MaxRoomsQuota == rooms && org.MaxItemsQuota == items) return;
            if (org.StripeSubscriptionId != observed.StripeSubscriptionId || org.SubscriptionPlan != observed.SubscriptionPlan ||
                org.SubscriptionStatus != observed.SubscriptionStatus)
                throw new DbUpdateConcurrencyException("The clinic's billing state changed during synchronization.");
            org.StripeSubscriptionId = subscriptionId;
            ApplyLocalPlanUpdate(org, plan, rooms, items, actor, status);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private string? PaidPlanFor(Stripe.Subscription subscription)
        {
            if (subscription.Items is not { HasMore: false, Data: { } items }) return null;
            var matching = new[] { "Starter", "Professional", "Enterprise" }.Where(tier =>
            {
                var priceId = _configuration[$"StripeSettings:{tier}PriceId"];
                return !string.IsNullOrWhiteSpace(priceId) && items.Any(i => i.Quantity > 0 && i.Price?.Id == priceId);
            }).ToArray();
            return matching.Length == 1 ? matching[0] : null;
        }

        private static (int Rooms, int Items) QuotasFor(string plan) => plan switch
        {
            "Free" => (2, 50),
            "Starter" => (5, 250),
            "Professional" => (15, 1500),
            "Enterprise" => (9999, 999999),
            _ => throw new ArgumentException("Unknown subscription tier.")
        };

        private void ApplyLocalPlanUpdate(Organization org, string plan, int roomsQuota, int itemsQuota, string updatedBy, string status = "Active")
        {
            org.SubscriptionPlan = plan;
            org.SubscriptionStatus = status;
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
