using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Features.Tenants;
using SaaS.Tenant.Service.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.MapTenantEndpoints();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Tenant" }));

app.Run();

public partial class Program { }
