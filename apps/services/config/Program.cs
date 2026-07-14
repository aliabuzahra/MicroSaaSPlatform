using Microsoft.EntityFrameworkCore;
using SaaS.Config.Service.Features.Settings;
using SaaS.Config.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.BuildingBlocks;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Shared.Kernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HeaderTenantContext>();

builder.Services.AddDbContext<ConfigDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var postgresConn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddServiceHealthChecks(postgresConnectionString: postgresConn);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantAuthorization();
app.UseServiceHealthChecks();

app.MapSettingsEndpoints();

app.Run();

public partial class Program { }
