using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaaS.Identity.Service.Domain.Entities;
using SaaS.Identity.Service.Infrastructure.Auth;
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

        group.MapPost("/register", async (
            RegisterRequest request, 
            IdentityDbContext db, 
            IPublishEndpoint publishEndpoint,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings) =>
        {
            var tenantIdToCheck = request.TenantId.HasValue ? new TenantId(request.TenantId.Value) : TenantId.New();
            if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email && u.TenantId == tenantIdToCheck))
            {
                return Results.Conflict("Email already exists for this tenant.");
            }

            var tenantId = request.TenantId.HasValue ? new TenantId(request.TenantId.Value) : TenantId.New();
            var passwordHash = passwordHasher.Hash(request.Password);

            var userResult = User.Create(request.Email, passwordHash, request.FullName, tenantId);
            
            if (userResult.IsFailure)
            {
                return Results.BadRequest(userResult.Error);
            }

            var user = userResult.Value;
            db.Users.Add(user);

            var refreshTokenStr = jwtService.GenerateRefreshToken();
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenStr, jwtSettings.Value.RefreshTokenExpirationDays);
            db.RefreshTokens.Add(refreshToken);

            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new UserRegisteredEvent(user.Id, user.Email, user.FullName ?? "User"));

            var accessToken = jwtService.GenerateAccessToken(user);

            return Results.Ok(new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshTokenStr,
                ExpiresIn: jwtSettings.Value.AccessTokenExpirationMinutes * 60,
                User: new UserDto(user.Id, user.Email, user.FullName, user.Role, user.TenantId.Value)
            ));
        });

        group.MapPost("/login", async (
            LoginRequest request, 
            IdentityDbContext db,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            if (!user.IsActive)
            {
                return Results.Problem("Account is deactivated.", statusCode: 403);
            }

            var refreshTokenStr = jwtService.GenerateRefreshToken();
            var refreshToken = RefreshToken.Create(user.Id, refreshTokenStr, jwtSettings.Value.RefreshTokenExpirationDays);
            db.RefreshTokens.Add(refreshToken);
            await db.SaveChangesAsync();

            var accessToken = jwtService.GenerateAccessToken(user);

            return Results.Ok(new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshTokenStr,
                ExpiresIn: jwtSettings.Value.AccessTokenExpirationMinutes * 60,
                User: new UserDto(user.Id, user.Email, user.FullName, user.Role, user.TenantId.Value)
            ));
        });

        group.MapPost("/refresh", async (
            RefreshRequest request,
            IdentityDbContext db,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings) =>
        {
            var existingToken = await db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

            if (existingToken is null || !existingToken.IsActive)
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == existingToken.UserId);
            if (user is null || !user.IsActive)
            {
                return Results.Unauthorized();
            }

            var newRefreshTokenStr = jwtService.GenerateRefreshToken();
            existingToken.Revoke(newRefreshTokenStr);

            var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenStr, jwtSettings.Value.RefreshTokenExpirationDays);
            db.RefreshTokens.Add(newRefreshToken);
            await db.SaveChangesAsync();

            var accessToken = jwtService.GenerateAccessToken(user);

            return Results.Ok(new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: newRefreshTokenStr,
                ExpiresIn: jwtSettings.Value.AccessTokenExpirationMinutes * 60,
                User: new UserDto(user.Id, user.Email, user.FullName, user.Role, user.TenantId.Value)
            ));
        });

        group.MapPost("/logout", async (
            RefreshRequest request,
            IdentityDbContext db) =>
        {
            var token = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == request.RefreshToken);
            if (token != null)
            {
                token.Revoke();
                await db.SaveChangesAsync();
            }
            return Results.Ok();
        });

        group.MapGet("/me", async (HttpContext httpContext, IdentityDbContext db) =>
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new UserDto(user.Id, user.Email, user.FullName, user.Role, user.TenantId.Value));
        }).RequireAuthorization();
    }

    public record RegisterRequest(string Email, string Password, string FullName, Guid? TenantId);
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, UserDto User);
    public record UserDto(Guid Id, string Email, string FullName, string Role, Guid TenantId);
}
