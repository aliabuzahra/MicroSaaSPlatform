using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Features.Billing;
using SaaS.Billing.Service.Features.Usage;
using SaaS.Billing.Service.Infrastructure.Paddle;
using SaaS.Billing.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddEventBus(builder.Configuration);
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<SaaS.Billing.Service.Features.Usage.UsageService>();

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRedisCache(builder.Configuration.GetConnectionString("Redis"));

var postgresConn = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConn = builder.Configuration.GetConnectionString("Redis");
var rabbitMqConn = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddServiceHealthChecks(
    postgresConnectionString: postgresConn,
    rabbitMqConnectionString: $"amqp://guest:guest@{rabbitMqConn}:5672",
    redisConnectionString: redisConn);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Auto-migrate for POC with Retry
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<BillingDbContext>();
    
    var retries = 5;
    while (retries > 0)
    {
        try
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Database initialized successfully.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning(ex, "Database not ready yet. Retrying... ({Retries} left)", retries);
            System.Threading.Thread.Sleep(2000);
        }
    }
}

app.UseHttpsRedirection();
app.UseServiceHealthChecks();

app.MapBillingEndpoints();
app.MapGetSubscriptionEndpoint();
app.MapUsageEndpoints();
app.MapPaddleWebhook();

app.Run();

public partial class Program { }
