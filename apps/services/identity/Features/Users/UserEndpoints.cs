using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Identity.Service.Domain.Entities;
using SaaS.Identity.Service.Infrastructure.Auth;
using SaaS.Identity.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").RequireAuthorization();

        group.MapGet("/", async (IdentityDbContext db, ITenantContext tenantContext) =>
        {
            var users = await db.Users
                .Select(u => new UserListDto(u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.CreatedAt))
                .ToListAsync();

            return Results.Ok(users);
        });

        group.MapGet("/{id:guid}", async (Guid id, IdentityDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new UserDetailDto(
                user.Id, 
                user.Email, 
                user.FullName, 
                user.Role, 
                user.IsActive, 
                user.CreatedAt,
                user.TenantId.Value
            ));
        });

        group.MapPut("/{id:guid}", async (
            Guid id, 
            UpdateUserRequest request, 
            IdentityDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                user.Role = request.Role;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new UserDetailDto(
                user.Id, 
                user.Email, 
                user.FullName, 
                user.Role, 
                user.IsActive, 
                user.CreatedAt,
                user.TenantId.Value
            ));
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, IdentityDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.IsActive = false;
            
            var tokens = await db.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ToListAsync();
            foreach (var token in tokens)
            {
                token.Revoke();
            }

            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IdentityDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.IsActive = true;
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapPost("/{id:guid}/change-password", async (
            Guid id, 
            ChangePasswordRequest request, 
            IdentityDbContext db,
            IPasswordHasher passwordHasher,
            HttpContext httpContext) =>
        {
            var currentUserId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != id.ToString())
            {
                return Results.Forbid();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Results.BadRequest("Current password is incorrect.");
            }

            user.PasswordHash = passwordHasher.Hash(request.NewPassword);
            
            var tokens = await db.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ToListAsync();
            foreach (var token in tokens)
            {
                token.Revoke();
            }

            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IdentityDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            var tokens = await db.RefreshTokens.Where(t => t.UserId == id).ToListAsync();
            db.RefreshTokens.RemoveRange(tokens);
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    public record UserListDto(Guid Id, string Email, string FullName, string Role, bool IsActive, DateTime CreatedAt);
    public record UserDetailDto(Guid Id, string Email, string FullName, string Role, bool IsActive, DateTime CreatedAt, Guid TenantId);
    public record UpdateUserRequest(string? FullName, string? Role);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
