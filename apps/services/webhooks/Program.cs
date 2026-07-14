using Microsoft.EntityFrameworkCore;
using SaaS.Webhooks.Service.Consumers;
using SaaS.Webhooks.Service.Features.Webhooks;
using SaaS.Webhooks.Service.Infrastructure.Persistence;
using SaaS.Webhooks.Service.Services;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.BuildingBlocks;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Shared.Kernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HeaderTenantContext>();

builder.Services.AddEventBus(builder.Configuration, [typeof(TenantCreatedWebhookConsumer).Assembly]);
builder.Services.AddHttpClient("webhook");
builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();

builder.Services.AddDbContext<WebhooksDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var postgresConn = builder.Configuration.GetConnectionString("DefaultConnection");
var rabbitMqConn = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddServiceHealthChecks(
    postgresConnectionString: postgresConn,
    rabbitMqConnectionString: $"amqp://guest:guest@{rabbitMqConn}:5672");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WebhooksDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantAuthorization();
app.UseServiceHealthChecks();

app.MapWebhookEndpoints();

app.Run();

public partial class Program { }
