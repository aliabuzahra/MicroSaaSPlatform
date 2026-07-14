using Microsoft.EntityFrameworkCore;
using SaaS.Identity.Service.Domain.Entities;
using SaaS.Identity.Service.Infrastructure.Persistence;
using SaaS.Identity.Service.Services;

using MassTransit;
using Microsoft.Extensions.Options;
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
            IPasswordService passwordService) =>
        {
            var normalizedEmail = request.Email.ToLowerInvariant();
            
            if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == normalizedEmail))
            {
                return Results.Conflict(new { Error = "Email already exists." });
            }

            if (!IsValidPassword(request.Password))
            {
                return Results.BadRequest(new { Error = "Password must be at least 8 characters with uppercase, lowercase, and number." });
            }

            var tenantId = request.TenantId.HasValue ? new TenantId(request.TenantId.Value) : TenantId.New();
            var passwordHash = passwordService.HashPassword(request.Password);

            var userResult = User.Create(normalizedEmail, passwordHash, request.FullName, tenantId);
            
            if (userResult.IsFailure)
            {
                return Results.BadRequest(new { Error = userResult.Error });
            }

            db.Users.Add(userResult.Value);
            
            var verificationToken = EmailVerificationToken.Create(userResult.Value.Id);
            db.EmailVerificationTokens.Add(verificationToken);
            
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new UserRegisteredEvent(
                userResult.Value.Id, 
                userResult.Value.Email, 
                userResult.Value.FullName ?? "User"));

            return Results.Created($"/users/{userResult.Value.Id}", new 
            { 
                userResult.Value.Id, 
                userResult.Value.Email,
                Message = "Registration successful. Please verify your email."
            });
        });

        group.MapPost("/login", async (
            LoginRequest request, 
            IdentityDbContext db,
            IJwtService jwtService,
            IPasswordService passwordService,
            IOptions<JwtSettings> jwtSettings) =>
        {
            var normalizedEmail = request.Email.ToLowerInvariant();
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            
            if (user is null || !passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            if (!user.IsActive)
            {
                return Results.Problem(
                    detail: "Account is deactivated.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var accessToken = jwtService.GenerateAccessToken(user);
            var refreshToken = jwtService.GenerateRefreshToken();
            
            var refreshTokenEntity = RefreshToken.Create(
                refreshToken, 
                user.Id, 
                jwtSettings.Value.RefreshTokenExpirationDays);
            
            db.RefreshTokens.Add(refreshTokenEntity);
            user.UpdateLastLogin();
            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponse(
                accessToken, 
                refreshToken, 
                user.Id, 
                user.Email, 
                user.Role,
                user.FullName,
                user.TenantId.Value,
                user.EmailVerified));
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            IdentityDbContext db,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings) =>
        {
            var existingToken = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (existingToken is null || !existingToken.IsActive)
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == existingToken.UserId);
            if (user is null || !user.IsActive)
            {
                return Results.Unauthorized();
            }

            var newAccessToken = jwtService.GenerateAccessToken(user);
            var newRefreshToken = jwtService.GenerateRefreshToken();
            
            existingToken.Revoke(newRefreshToken);
            
            var newRefreshTokenEntity = RefreshToken.Create(
                newRefreshToken, 
                user.Id, 
                jwtSettings.Value.RefreshTokenExpirationDays);
            
            db.RefreshTokens.Add(newRefreshTokenEntity);
            await db.SaveChangesAsync();

            return Results.Ok(new RefreshResponse(newAccessToken, newRefreshToken));
        });

        group.MapPost("/logout", async (
            LogoutRequest request,
            IdentityDbContext db) =>
        {
            var refreshToken = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (refreshToken is not null)
            {
                refreshToken.Revoke();
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { Message = "Logged out successfully." });
        });

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            IdentityDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var normalizedEmail = request.Email.ToLowerInvariant();
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            
            if (user is not null)
            {
                var existingTokens = await db.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && t.UsedAt == null)
                    .ToListAsync();
                
                foreach (var token in existingTokens)
                {
                    token.MarkAsUsed();
                }

                var resetToken = PasswordResetToken.Create(user.Id);
                db.PasswordResetTokens.Add(resetToken);
                await db.SaveChangesAsync();

                await publishEndpoint.Publish(new PasswordResetRequestedEvent(
                    user.Id, 
                    user.Email, 
                    resetToken.Token));
            }

            return Results.Ok(new { Message = "If the email exists, a password reset link has been sent." });
        });

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            IdentityDbContext db,
            IPasswordService passwordService) =>
        {
            var resetToken = await db.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            if (resetToken is null || !resetToken.IsValid)
            {
                return Results.BadRequest(new { Error = "Invalid or expired reset token." });
            }

            if (!IsValidPassword(request.NewPassword))
            {
                return Results.BadRequest(new { Error = "Password must be at least 8 characters with uppercase, lowercase, and number." });
            }

            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == resetToken.UserId);
            if (user is null)
            {
                return Results.BadRequest(new { Error = "Invalid reset token." });
            }

            user.UpdatePassword(passwordService.HashPassword(request.NewPassword));
            resetToken.MarkAsUsed();

            var activeRefreshTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync();
            
            foreach (var token in activeRefreshTokens)
            {
                token.Revoke();
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Password reset successfully." });
        });

        group.MapPost("/verify-email", async (
            VerifyEmailRequest request,
            IdentityDbContext db) =>
        {
            var verificationToken = await db.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            if (verificationToken is null || !verificationToken.IsValid)
            {
                return Results.BadRequest(new { Error = "Invalid or expired verification token." });
            }

            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == verificationToken.UserId);
            if (user is null)
            {
                return Results.BadRequest(new { Error = "Invalid verification token." });
            }

            user.VerifyEmail();
            verificationToken.MarkAsVerified();
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Email verified successfully." });
        });

        group.MapPost("/resend-verification", async (
            ResendVerificationRequest request,
            IdentityDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var normalizedEmail = request.Email.ToLowerInvariant();
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user is not null && !user.EmailVerified)
            {
                var existingTokens = await db.EmailVerificationTokens
                    .Where(t => t.UserId == user.Id && t.VerifiedAt == null)
                    .ToListAsync();
                
                foreach (var token in existingTokens)
                {
                    token.MarkAsVerified();
                }

                var verificationToken = EmailVerificationToken.Create(user.Id);
                db.EmailVerificationTokens.Add(verificationToken);
                await db.SaveChangesAsync();

                await publishEndpoint.Publish(new EmailVerificationRequestedEvent(
                    user.Id, 
                    user.Email, 
                    verificationToken.Token));
            }

            return Results.Ok(new { Message = "If the email exists and is not verified, a verification link has been sent." });
        });

        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            IdentityDbContext db,
            IPasswordService passwordService) =>
        {
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == request.UserId);
            
            if (user is null || !passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Results.BadRequest(new { Error = "Current password is incorrect." });
            }

            if (!IsValidPassword(request.NewPassword))
            {
                return Results.BadRequest(new { Error = "Password must be at least 8 characters with uppercase, lowercase, and number." });
            }

            user.UpdatePassword(passwordService.HashPassword(request.NewPassword));

            var activeRefreshTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync();
            
            foreach (var token in activeRefreshTokens)
            {
                token.Revoke();
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Password changed successfully. Please login again." });
        });

        group.MapGet("/me", async (
            HttpContext context,
            IdentityDbContext db) =>
        {
            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new UserProfileResponse(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                user.TenantId.Value,
                user.EmailVerified,
                user.CreatedAt,
                user.LastLoginAt));
        }).RequireAuthorization();
    }

    private static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);

        return hasUpper && hasLower && hasDigit;
    }

    public record RegisterRequest(string Email, string Password, string FullName, Guid? TenantId);
    public record LoginRequest(string Email, string Password);
    public record RefreshTokenRequest(string RefreshToken);
    public record LogoutRequest(string RefreshToken);
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Token, string NewPassword);
    public record VerifyEmailRequest(string Token);
    public record ResendVerificationRequest(string Email);
    public record ChangePasswordRequest(Guid UserId, string CurrentPassword, string NewPassword);

    public record AuthResponse(
        string AccessToken, 
        string RefreshToken, 
        Guid UserId, 
        string Email, 
        string Role,
        string FullName,
        Guid TenantId,
        bool EmailVerified);
    
    public record RefreshResponse(string AccessToken, string RefreshToken);
    
    public record UserProfileResponse(
        Guid Id,
        string Email,
        string FullName,
        string Role,
        Guid TenantId,
        bool EmailVerified,
        DateTime CreatedAt,
        DateTime? LastLoginAt);
}
