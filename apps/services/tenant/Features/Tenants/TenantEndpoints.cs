using MassTransit;
using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Domain.Entities;
using SaaS.Tenant.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Events;

namespace SaaS.Tenant.Service.Features.Tenants;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var tenantsGroup = app.MapGroup("/tenants");
        var membersGroup = app.MapGroup("/tenants/{tenantId:guid}/members");
        var invitationsGroup = app.MapGroup("/tenants/{tenantId:guid}/invitations");

        tenantsGroup.MapPost("/", async (
            CreateTenantRequest request, 
            TenantDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var normalizedSlug = request.Slug.ToLowerInvariant();
            
            if (await db.Tenants.AnyAsync(t => t.Slug == normalizedSlug))
            {
                return Results.Conflict(new { Error = "Tenant slug already exists." });
            }

            var tenantResult = Domain.Entities.Tenant.Create(request.Name, normalizedSlug, request.Email);
            
            if (tenantResult.IsFailure)
            {
                return Results.BadRequest(new { Error = tenantResult.Error });
            }

            db.Tenants.Add(tenantResult.Value);
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new TenantCreatedEvent(
                tenantResult.Value.Id,
                tenantResult.Value.Name,
                tenantResult.Value.Slug,
                tenantResult.Value.ContactEmail));

            return Results.Created($"/tenants/{tenantResult.Value.Id}", new TenantResponse(tenantResult.Value));
        });

        tenantsGroup.MapGet("/", async (TenantDbContext db, int? page, int? pageSize, bool? activeOnly) =>
        {
            var query = db.Tenants.AsQueryable();
            
            if (activeOnly == true)
            {
                query = query.Where(t => t.IsActive);
            }

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 20, 100);
            
            var tenants = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(t => new TenantResponse(t))
                .ToListAsync();

            return Results.Ok(new PaginatedResponse<TenantResponse>(tenants, totalCount, actualPage, actualPageSize));
        });

        tenantsGroup.MapGet("/{id:guid}", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            return Results.Ok(new TenantResponse(tenant));
        });

        tenantsGroup.MapGet("/by-slug/{slug}", async (string slug, TenantDbContext db) =>
        {
            var normalizedSlug = slug.ToLowerInvariant();
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == normalizedSlug);
            
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            return Results.Ok(new TenantResponse(tenant));
        });

        tenantsGroup.MapPut("/{id:guid}", async (
            Guid id, 
            UpdateTenantRequest request, 
            TenantDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            var updateResult = tenant.Update(request.Name, request.Email, request.LogoUrl, request.Description);
            
            if (updateResult.IsFailure)
            {
                return Results.BadRequest(new { Error = updateResult.Error });
            }

            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new TenantUpdatedEvent(
                tenant.Id,
                tenant.Name,
                tenant.ContactEmail,
                tenant.SubscriptionPlan));

            return Results.Ok(new TenantResponse(tenant));
        });

        tenantsGroup.MapDelete("/{id:guid}", async (
            Guid id, 
            TenantDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            tenant.Deactivate();
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new TenantDeactivatedEvent(tenant.Id, tenant.Name));

            return Results.Ok(new { Message = "Tenant deactivated successfully." });
        });

        tenantsGroup.MapPost("/{id:guid}/activate", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            tenant.Activate();
            await db.SaveChangesAsync();

            return Results.Ok(new TenantResponse(tenant));
        });

        membersGroup.MapGet("/", async (Guid tenantId, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            var members = await db.TenantMembers
                .Where(m => m.TenantId == tenantId && m.IsActive)
                .OrderByDescending(m => m.JoinedAt)
                .Select(m => new TenantMemberResponse(m))
                .ToListAsync();

            return Results.Ok(members);
        });

        membersGroup.MapPost("/", async (
            Guid tenantId, 
            AddMemberRequest request, 
            TenantDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            var normalizedEmail = request.Email.ToLowerInvariant();
            var existingMember = await db.TenantMembers
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Email == normalizedEmail);
            
            if (existingMember is not null)
            {
                if (existingMember.IsActive)
                {
                    return Results.Conflict(new { Error = "User is already a member of this tenant." });
                }
                existingMember.IsActive = true;
                existingMember.Role = request.Role ?? TenantRoles.Member;
            }
            else
            {
                var role = request.Role ?? TenantRoles.Member;
                if (!TenantRoles.IsValid(role))
                {
                    return Results.BadRequest(new { Error = $"Invalid role. Valid roles are: {string.Join(", ", TenantRoles.All)}" });
                }

                var member = TenantMember.Create(tenantId, request.UserId, normalizedEmail, role);
                db.TenantMembers.Add(member);
            }

            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new TenantMemberAddedEvent(
                tenantId, 
                request.UserId, 
                normalizedEmail, 
                request.Role ?? TenantRoles.Member));

            return Results.Created($"/tenants/{tenantId}/members/{request.UserId}", new { Message = "Member added successfully." });
        });

        membersGroup.MapPut("/{userId:guid}", async (
            Guid tenantId, 
            Guid userId, 
            UpdateMemberRequest request, 
            TenantDbContext db) =>
        {
            var member = await db.TenantMembers
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId);
            
            if (member is null)
            {
                return Results.NotFound(new { Error = "Member not found." });
            }

            if (!TenantRoles.IsValid(request.Role))
            {
                return Results.BadRequest(new { Error = $"Invalid role. Valid roles are: {string.Join(", ", TenantRoles.All)}" });
            }

            member.UpdateRole(request.Role);
            await db.SaveChangesAsync();

            return Results.Ok(new TenantMemberResponse(member));
        });

        membersGroup.MapDelete("/{userId:guid}", async (
            Guid tenantId, 
            Guid userId, 
            TenantDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var member = await db.TenantMembers
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId);
            
            if (member is null)
            {
                return Results.NotFound(new { Error = "Member not found." });
            }

            member.Deactivate();
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new TenantMemberRemovedEvent(tenantId, userId));

            return Results.Ok(new { Message = "Member removed successfully." });
        });

        invitationsGroup.MapPost("/", async (
            Guid tenantId, 
            CreateInvitationRequest request, 
            TenantDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            var normalizedEmail = request.Email.ToLowerInvariant();
            
            var existingMember = await db.TenantMembers
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Email == normalizedEmail && m.IsActive);
            
            if (existingMember is not null)
            {
                return Results.Conflict(new { Error = "User is already a member of this tenant." });
            }

            var pendingInvitation = await db.TenantInvitations
                .FirstOrDefaultAsync(i => i.TenantId == tenantId && 
                                          i.Email == normalizedEmail && 
                                          i.AcceptedAt == null && 
                                          i.DeclinedAt == null &&
                                          i.ExpiresAt > DateTime.UtcNow);
            
            if (pendingInvitation is not null)
            {
                return Results.Conflict(new { Error = "An active invitation already exists for this email." });
            }

            var role = request.Role ?? TenantRoles.Member;
            if (!TenantRoles.IsValid(role))
            {
                return Results.BadRequest(new { Error = $"Invalid role. Valid roles are: {string.Join(", ", TenantRoles.All)}" });
            }

            var invitation = TenantInvitation.Create(tenantId, normalizedEmail, role, request.InvitedByUserId);
            db.TenantInvitations.Add(invitation);
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new TenantInvitationSentEvent(
                tenantId, 
                tenant.Name,
                normalizedEmail, 
                invitation.Token,
                role));

            return Results.Created($"/tenants/{tenantId}/invitations/{invitation.Id}", new InvitationResponse(invitation, tenant.Name));
        });

        invitationsGroup.MapGet("/", async (Guid tenantId, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return Results.NotFound(new { Error = "Tenant not found." });
            }

            var invitations = await db.TenantInvitations
                .Where(i => i.TenantId == tenantId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new InvitationResponse(i, tenant.Name))
                .ToListAsync();

            return Results.Ok(invitations);
        });

        app.MapPost("/invitations/accept", async (
            AcceptInvitationRequest request, 
            TenantDbContext db,
            IPublishEndpoint publishEndpoint) =>
        {
            var invitation = await db.TenantInvitations
                .FirstOrDefaultAsync(i => i.Token == request.Token);
            
            if (invitation is null)
            {
                return Results.NotFound(new { Error = "Invitation not found." });
            }

            if (!invitation.IsPending)
            {
                return Results.BadRequest(new { Error = "Invitation is no longer valid." });
            }

            invitation.Accept();

            var member = TenantMember.Create(
                invitation.TenantId, 
                request.UserId, 
                invitation.Email, 
                invitation.Role);
            
            db.TenantMembers.Add(member);
            await db.SaveChangesAsync();

            await publishEndpoint.Publish(new TenantMemberAddedEvent(
                invitation.TenantId, 
                request.UserId, 
                invitation.Email, 
                invitation.Role));

            return Results.Ok(new { Message = "Invitation accepted successfully.", TenantId = invitation.TenantId });
        });

        app.MapPost("/invitations/decline", async (DeclineInvitationRequest request, TenantDbContext db) =>
        {
            var invitation = await db.TenantInvitations
                .FirstOrDefaultAsync(i => i.Token == request.Token);
            
            if (invitation is null)
            {
                return Results.NotFound(new { Error = "Invitation not found." });
            }

            if (!invitation.IsPending)
            {
                return Results.BadRequest(new { Error = "Invitation is no longer valid." });
            }

            invitation.Decline();
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Invitation declined." });
        });
    }

    public record CreateTenantRequest(string Name, string Slug, string? Email);
    public record UpdateTenantRequest(string? Name, string? Email, string? LogoUrl, string? Description);
    public record AddMemberRequest(Guid UserId, string Email, string? Role);
    public record UpdateMemberRequest(string Role);
    public record CreateInvitationRequest(string Email, string? Role, Guid InvitedByUserId);
    public record AcceptInvitationRequest(string Token, Guid UserId);
    public record DeclineInvitationRequest(string Token);

    public record TenantResponse(
        Guid Id, 
        string Name, 
        string Slug, 
        string SubscriptionPlan,
        bool IsActive,
        string? ContactEmail,
        string? LogoUrl,
        string? Description,
        DateTime CreatedAt,
        DateTime? UpdatedAt)
    {
        public TenantResponse(Domain.Entities.Tenant tenant) : this(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.SubscriptionPlan,
            tenant.IsActive,
            tenant.ContactEmail,
            tenant.LogoUrl,
            tenant.Description,
            tenant.CreatedAt,
            tenant.UpdatedAt)
        { }
    }

    public record TenantMemberResponse(
        Guid Id,
        Guid TenantId,
        Guid UserId,
        string Email,
        string Role,
        DateTime JoinedAt,
        bool IsActive)
    {
        public TenantMemberResponse(TenantMember member) : this(
            member.Id,
            member.TenantId,
            member.UserId,
            member.Email,
            member.Role,
            member.JoinedAt,
            member.IsActive)
        { }
    }

    public record InvitationResponse(
        Guid Id,
        Guid TenantId,
        string TenantName,
        string Email,
        string Role,
        string Status,
        DateTime CreatedAt,
        DateTime ExpiresAt)
    {
        public InvitationResponse(TenantInvitation invitation, string tenantName) : this(
            invitation.Id,
            invitation.TenantId,
            tenantName,
            invitation.Email,
            invitation.Role,
            invitation.IsPending ? "Pending" : invitation.IsAccepted ? "Accepted" : invitation.IsDeclined ? "Declined" : "Expired",
            invitation.CreatedAt,
            invitation.ExpiresAt)
        { }
    }

    public record PaginatedResponse<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
