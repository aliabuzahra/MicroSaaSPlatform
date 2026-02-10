using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Domain.Entities;
using SaaS.Tenant.Service.Infrastructure.Persistence;

namespace SaaS.Tenant.Service.Features.Tenants;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tenants");

        group.MapPost("/", async (CreateTenantRequest request, TenantDbContext db) =>
        {
            if (await db.Tenants.AnyAsync(t => t.Slug == request.Slug))
            {
                return Results.Conflict("Tenant slug already exists.");
            }

            var tenantResult = Domain.Entities.Tenant.Create(request.Name, request.Slug, request.Email);
            
            if (tenantResult.IsFailure)
            {
                return Results.BadRequest(tenantResult.Error);
            }

            db.Tenants.Add(tenantResult.Value);
            await db.SaveChangesAsync();

            return Results.Created($"/tenants/{tenantResult.Value.Id}", tenantResult.Value);
        });

        group.MapGet("/", async (TenantDbContext db) =>
        {
            // For POC: List all tenants (in real world, this would be admin only or filtered)
            return Results.Ok(await db.Tenants.ToListAsync());
        });
    }

    public record CreateTenantRequest(string Name, string Slug, string? Email);
}
