using Microsoft.EntityFrameworkCore;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Audit.Service.Consumers;
using SaaS.Audit.Service.Features;
using SaaS.Audit.Service.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEventBus(builder.Configuration, [typeof(UserRegisteredConsumer).Assembly]);

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.MapAuditEndpoints();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Audit" }));

app.Run();

public partial class Program { }
