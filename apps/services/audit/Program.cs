using Microsoft.EntityFrameworkCore;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Audit.Service.Consumers;
using SaaS.Audit.Service.Infrastructure;
using SaaS.Audit.Service.Infrastructure.Persistence;
using SaaS.Audit.Service.Features.Audit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEventBus(builder.Configuration, [typeof(UserRegisteredConsumer).Assembly]);

builder.Services.AddScoped<IAuditLogger, AuditLogger>();

builder.Services.AddDbContext<AuditDbContext>(options =>
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
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseServiceHealthChecks();

app.MapAuditEndpoints();

app.Run();

public partial class Program { }
