using Microsoft.EntityFrameworkCore;
using SaaS.Storage.Service.Features.Files;
using SaaS.Storage.Service.Infrastructure.Persistence;
using SaaS.Storage.Service.Infrastructure.Storage;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.BuildingBlocks;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Shared.Kernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HeaderTenantContext>();

builder.Services.AddSingleton<IStorageProvider, LocalStorageProvider>();

builder.Services.AddDbContext<StorageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var postgresConn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddServiceHealthChecks(postgresConnectionString: postgresConn);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantAuthorization();
app.UseServiceHealthChecks();

app.MapFileEndpoints();

app.Run();

public partial class Program { }
