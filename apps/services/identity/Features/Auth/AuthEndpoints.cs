using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Identity.Service.Domain.Entities;
using SaaS.Identity.Service.Infrastructure.Persistence;

using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (RegisterRequest request, IdentityDbContext db, IPublishEndpoint publishEndpoint) =>
        {
            if (await db.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Results.Conflict("Email already exists.");
            }

            var tenantId = request.TenantId.HasValue ? new TenantId(request.TenantId.Value) : TenantId.New();

            // In a real app, hash the password here (e.g., BCrypt).
            var userResult = User.Create(request.Email, request.Password, request.FullName, tenantId);
            
            if (userResult.IsFailure)
            {
                return Results.BadRequest(userResult.Error);
            }

            db.Users.Add(userResult.Value);
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new UserRegisteredEvent(userResult.Value.Id, userResult.Value.Email, userResult.Value.FullName ?? "User"));

            return Results.Ok(new { userResult.Value.Id, userResult.Value.Email });
        });

        group.MapPost("/login", async (LoginRequest request, IdentityDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user is null || user.PasswordHash != request.Password) // Simplified for POC
            {
                return Results.Unauthorized();
            }

            // In a real app, generate JWT here.
            return Results.Ok(new { Token = "dummy-jwt-token", user.Id, user.Email, user.Role });
        });
    }

    public record RegisterRequest(string Email, string Password, string FullName, Guid? TenantId);
    public record LoginRequest(string Email, string Password);
}
