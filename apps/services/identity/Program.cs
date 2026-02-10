using Microsoft.EntityFrameworkCore;
using SaaS.Identity.Service.Features.Auth;
using SaaS.Identity.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Shared.Kernel.BuildingBlocks;
using SaaS.Shared.Kernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEventBus(builder.Configuration); // Producer only
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HeaderTenantContext>();

// Infrastructure
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Auto-migrate for POC convenience (CAUTION: Don't use in prod)
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.MapAuthEndpoints();

app.Run();

public partial class Program { }
