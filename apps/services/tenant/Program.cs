using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Consumers;
using SaaS.Tenant.Service.Features.Tenants;
using SaaS.Tenant.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddEventBus(builder.Configuration, [typeof(SubscriptionStatusChangedConsumer).Assembly]);

builder.Services.AddDbContext<TenantDbContext>(options =>
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
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseServiceHealthChecks();

app.MapTenantEndpoints();

app.Run();

public partial class Program { }
