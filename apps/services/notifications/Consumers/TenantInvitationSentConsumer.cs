using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Email;

namespace SaaS.Notifications.Service.Consumers;

public class TenantInvitationSentConsumer : IConsumer<TenantInvitationSentEvent>
{
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<TenantInvitationSentConsumer> _logger;
    private readonly IConfiguration _configuration;

    public TenantInvitationSentConsumer(
        INotificationSender notificationSender,
        ILogger<TenantInvitationSentConsumer> logger,
        IConfiguration configuration)
    {
        _notificationSender = notificationSender;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<TenantInvitationSentEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing tenant invitation for {Email} to {TenantName}", message.Email, message.TenantName);

        try
        {
            var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
            var invitationUrl = $"{baseUrl}/accept-invitation?token={message.InvitationToken}";

            var variables = new Dictionary<string, string>
            {
                { "tenant_name", message.TenantName },
                { "email", message.Email },
                { "role", message.Role },
                { "invitation_url", invitationUrl }
            };

            await _notificationSender.SendNotificationAsync(
                Guid.Empty,
                message.TenantId,
                NotificationTypes.TenantInvitation,
                message.Email,
                variables,
                context.CancellationToken);

            _logger.LogInformation("Tenant invitation email sent to {Email}", message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tenant invitation to {Email}", message.Email);
            throw;
        }
    }
}
