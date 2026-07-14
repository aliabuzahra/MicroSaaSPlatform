using Microsoft.EntityFrameworkCore;
using SaaS.Features.Service.Features.FeatureFlags;
using SaaS.Features.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddDbContext<FeaturesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var postgresConn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddServiceHealthChecks(postgresConnectionString: postgresConn);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FeaturesDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseServiceHealthChecks();

app.MapFeatureFlagEndpoints();

app.Run();

public partial class Program { }
