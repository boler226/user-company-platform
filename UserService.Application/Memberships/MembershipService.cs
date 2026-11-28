using Platform.ServiceDefaults.Caching;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data;
using AutoMapper;
using UserService.Domain.Memberships;
using UserService.Infrastructure.Memberships;

namespace UserService.Application.Memberships;

public sealed class MembershipService(
    IMembershipUnitOfWork unitOfWork,
    ICompanySource companies,
    IMapper mapper,
    ILogger<MembershipService> logger,
    RedisReadCache cache) : IMembershipService
{
    private static readonly ActivitySource Activities = new("UserService.Business");

    public async Task<MembershipDto> GetAsync(Guid id, CancellationToken ct)
    {
        using var activity = Activities.StartActivity("memberships.get");
        activity?.SetTag("membership.id", id);
        var membership = await unitOfWork.Memberships.GetWithHistoryAsync(id, ct)
            ?? throw new ResourceNotFoundException("Membership not found.");
        return mapper.Map<MembershipDto>(membership);
    }

    public async Task<IReadOnlyList<MembershipDto>> ListAsync(Guid userId, int page, CancellationToken ct)
    {
        using var activity = Activities.StartActivity("memberships.list");
        activity?.SetTag("user.id", userId);
        if (userId == Guid.Empty || page is < 1 or > 100000)
        {
            throw new InputValidationException("Supply a user ID and a page between 1 and 100000.");
        }
        var memberships = await unitOfWork.Memberships.FindByUserAsync(userId, page, ct);
        logger.LogInformation("Loaded membership page {Page} for {UserId}", page, userId);
        return mapper.Map<List<MembershipDto>>(memberships);
    }

    public async Task<MembershipDto> CreateAsync(CreateMembershipRequest input, CancellationToken ct)
    {
        using var activity = Activities.StartActivity("memberships.create");
        activity?.SetTag("company.id", input.CompanyId);
        ValidateRole(input.Role);
        if (input.UserId == Guid.Empty || input.CompanyId == Guid.Empty)
        {
            throw new InputValidationException("User and company IDs are required.");
        }
        if (!await unitOfWork.Users.ExistsAsync(input.UserId, ct))
        {
            throw new ResourceNotFoundException("User not found.");
        }

        // The HTTP request happens before opening a transaction, avoiding locks during network IO.
        var company = await companies.GetAsync(input.CompanyId, ct);
        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            UserId = input.UserId,
            CompanyId = input.CompanyId,
            RequestedRole = input.Role
        };

        await InTransactionAsync(async () =>
        {
            await unitOfWork.Companies.RefreshAsync(company, ct);
            await unitOfWork.Memberships.AddAsync(membership, ct);
        }, ct);

        await cache.InvalidateAsync(RedisReadCache.Memberships, "MembershipCreated");
        logger.LogInformation("Created membership {MembershipId} for company {CompanyId}", membership.Id, input.CompanyId);
        return await GetAsync(membership.Id, ct);
    }

    public async Task UpdateAsync(Guid id, EditMembershipRequest input, CancellationToken ct)
    {
        using var activity = Activities.StartActivity("memberships.update");
        activity?.SetTag("membership.id", id);
        ValidateRole(input.Role);
        ValidateVersion(input.Version);
        var membership = await FindAsync(id, ct);
        if (membership.Status != "Submitted")
        {
            throw new BusinessConflictException("Only submitted memberships can be edited.");
        }
        membership.RequestedRole = input.Role;
        membership.Version = input.Version;
        await unitOfWork.Memberships.UpdateAsync(membership, ct);
        await cache.InvalidateAsync(RedisReadCache.Memberships, "MembershipUpdated");
    }

    public async Task ChangeStatusAsync(Guid id, ChangeMembershipStatusRequest input, CancellationToken ct)
    {
        using var activity = Activities.StartActivity("memberships.change_status");
        activity?.SetTag("membership.id", id);
        ValidateVersion(input.Version);
        if (input.Status is not ("AwaitingValidation" or "Approved" or "Active" or "Cancelled"))
        {
            throw new InputValidationException("Unknown membership status.");
        }

        await InTransactionAsync(async () =>
        {
            var membership = await FindAsync(id, ct);
            var allowed = (membership.Status, input.Status) switch
            {
                ("Submitted", "AwaitingValidation") => true,
                ("AwaitingValidation", "Approved") => true,
                ("Approved", "Active") => true,
                ("Submitted" or "AwaitingValidation" or "Approved", "Cancelled") => true,
                _ => false
            };
            if (!allowed)
            {
                throw new BusinessConflictException("The requested status transition is not allowed.");
            }

            await unitOfWork.Memberships.ChangeStatusAsync(id, input.Status, input.Version, ct);

            if (input.Status == "Active")
            {
                // If the user belongs to another company, this fails AFTER the procedure.
                // The shared transaction rolls back both the status and its history entry.
                await unitOfWork.Users.AssignCompanyAsync(membership.UserId, membership.CompanyId, ct);
            }
        }, ct);
        await cache.InvalidateAsync(RedisReadCache.Memberships, "MembershipStatusChanged");
        activity?.SetTag("membership.status", input.Status);
        logger.LogInformation("Changed membership {MembershipId} to {Status}", id, input.Status);
    }

    public async Task DeleteAsync(Guid id, long version, CancellationToken ct)
    {
        ValidateVersion(version);
        var membership = await FindAsync(id, ct);
        if (membership.Status != "Cancelled")
        {
            throw new BusinessConflictException("Cancel the membership before deleting it.");
        }
        await unitOfWork.Memberships.DeleteAsync(id, version, ct);
        await cache.InvalidateAsync(RedisReadCache.Memberships, "MembershipDeleted");
    }

    private async Task<Membership> FindAsync(Guid id, CancellationToken ct) =>
        await unitOfWork.Memberships.GetByIdAsync(id, ct)
        ?? throw new ResourceNotFoundException("Membership not found.");

    private async Task InTransactionAsync(Func<Task> operation, CancellationToken ct)
    {
        await unitOfWork.BeginAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await operation();
            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            // Request cancellation must not prevent cleanup.
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void ValidateRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || role.Length > 50)
        {
            throw new InputValidationException("Role must contain between 1 and 50 characters.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw new InputValidationException("A positive record version is required.");
        }
    }
}
