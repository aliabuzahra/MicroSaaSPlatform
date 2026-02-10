using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MassTransit;
using SaaS.Shared.Kernel.IntegrationEvents.Billing;
using SaaS.Billing.Service.Features.Billing;

namespace SaaS.Billing.Service.Infrastructure.Paddle;

public static class PaddleWebhookEndpoint
{
    public static void MapPaddleWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/billing/webhook", async (HttpContext context, IConfiguration config, SubscriptionService subscriptionService, ILogger<Program> logger) =>
        {
            var signatureHeader = context.Request.Headers["Paddle-Signature"].ToString();
            var webhookSecret = config["Paddle:WebhookSecret"];

            if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(webhookSecret))
            {
                logger.LogWarning("Missing Paddle Webhook Secret or Signature");
                return Results.BadRequest();
            }

            // Read raw body
            using var reader = new StreamReader(context.Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            if (!VerifySignature(signatureHeader, rawBody, webhookSecret))
            {
                logger.LogWarning("Invalid Paddle Signature");
                return Results.BadRequest();
            }

            try
            {
                var jsonNode = JsonNode.Parse(rawBody);
                var eventType = jsonNode?["event_type"]?.ToString();
                var data = jsonNode?["data"];

                logger.LogInformation("Received Paddle Event: {EventType}", eventType);

                if (data is null) return Results.Ok();

                string? status = null;
                string? tenantIdStr = null;
                string? priceId = null;
                DateTime? nextBillDate = null;
                string? updateUrl = null;
                string? cancelUrl = null;

                // Paddle v2 Event Structure
                switch (eventType)
                {
                    case "subscription.activated":
                    case "subscription.updated":
                    case "subscription.canceled":
                        status = data["status"]?.ToString();
                        
                        // Custom Data
                        tenantIdStr = data["custom_data"]?["tenantId"]?.ToString();
                        
                        // Price ID (Plan)
                        var items = data["items"]?.AsArray();
                        if (items != null && items.Count > 0)
                        {
                            priceId = items[0]?["price"]?["id"]?.ToString();
                        }

                        // Next Bill Date
                        var nextBilledAtStr = data["next_billed_at"]?.ToString();
                        if (DateTime.TryParse(nextBilledAtStr, out var nbd))
                        {
                            nextBillDate = nbd;
                        }

                        // Management URLs
                        var urls = data["management_urls"];
                        updateUrl = urls?["update"]?.ToString();
                        cancelUrl = urls?["cancel"]?.ToString();
                        break;
                }

                if (!string.IsNullOrEmpty(status) && Guid.TryParse(tenantIdStr, out var tenantId))
                {
                    await subscriptionService.HandleSubscriptionUpdateAsync(
                        tenantId, 
                        status, 
                        priceId ?? "unknown", 
                        data?["id"]?.ToString() ?? "unknown", // SubscriptionId
                        nextBillDate, 
                        updateUrl, 
                        cancelUrl
                    );
                }

                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Paddle webhook");
                return Results.StatusCode(500);
            }
        });
    }

    private static bool VerifySignature(string signatureHeader, string rawBody, string secret)
    {
        // Header format: ts=123456789;h1=abcdef...
        var parts = signatureHeader.Split(';');
        var tsPart = parts.FirstOrDefault(p => p.StartsWith("ts="))?.Substring(3);
        var h1Part = parts.FirstOrDefault(p => p.StartsWith("h1="))?.Substring(3);

        if (string.IsNullOrEmpty(tsPart) || string.IsNullOrEmpty(h1Part)) return false;

        // Prevent Replay Attacks (e.g., 5 mins tolerance)
        // Note: For dev/testing we might skip strict time checks, but good to have.
        
        var payload = $"{tsPart}:{rawBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return hashString == h1Part;
    }

    private static string MapPaddleStatus(string paddleStatus)
    {
        return paddleStatus switch
        {
            "active" => "active",
            "trialing" => "trialing",
            "past_due" => "past_due",
            "paused" => "paused",
            "canceled" => "canceled",
            _ => "unknown"
        };
    }
}
