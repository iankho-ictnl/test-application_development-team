using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Logic.V1.Users;

public sealed class UserService(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    AppDbContext db) : IUserService
{
    public async Task<IReadOnlyList<UserModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        var list = await users.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(cancellationToken);
        var result = new List<UserModel>();
        foreach (var user in list) result.Add(await ToModelAsync(user, cancellationToken));
        return result;
    }

    // Identity's UserManager takes no cancellation token, so each step checks it before it starts.
    public async Task<UserModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(id);
        return user is null ? null : await ToModelAsync(user, cancellationToken);
    }

    public async Task<UserModel> CreateAsync(UserCreateModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ValidateRolesAndOwnerAsync(model.Roles, model.OwnerId, cancellationToken);
        var user = new ApplicationUser { UserName = model.UserName, Email = model.Email, EmailConfirmed = true, OwnerId = model.OwnerId };
        EnsureSucceeded(await users.CreateAsync(user, model.Password));
        await users.AddToRolesAsync(user, model.Roles.Distinct());
        return await ToModelAsync(user, cancellationToken);
    }

    public async Task<UserModel?> UpdateAsync(string id, UserUpdateModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(id);
        if (user is null) return null;
        await ValidateRolesAndOwnerAsync(model.Roles, model.OwnerId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        user.Email = model.Email;
        user.OwnerId = model.OwnerId;
        await users.SetLockoutEndDateAsync(user, model.LockedOut ? DateTimeOffset.MaxValue : null);
        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current.Except(model.Roles));
        await users.AddToRolesAsync(user, model.Roles.Distinct().Except(current));
        await users.UpdateSecurityStampAsync(user);
        EnsureSucceeded(await users.UpdateAsync(user));
        await UserAdminGuard.EnsureRemainsAsync(db, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToModelAsync(user, cancellationToken);
    }

    public async Task<bool> ResetTwoFactorAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(id);
        if (user is null) return false;
        await users.SetTwoFactorEnabledAsync(user, false);
        await users.ResetAuthenticatorKeyAsync(user);
        return true;
    }

    public async Task<bool> ForceSignOutAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(id);
        if (user is null) return false;
        await users.UpdateSecurityStampAsync(user);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(id);
        if (user is null) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await users.DeleteAsync(user);
        await UserAdminGuard.EnsureRemainsAsync(db, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task ValidateRolesAndOwnerAsync(IReadOnlyList<string> roleNames, int? ownerId, CancellationToken cancellationToken)
    {
        foreach (var roleName in roleNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await roles.RoleExistsAsync(roleName)) throw new BusinessRuleException($"Rol '{roleName}' bestaat niet.");
        }
        if (ownerId is int id && !await db.Owners.AnyAsync(owner => owner.Id == id, cancellationToken))
            throw new BusinessRuleException("Eigenaar bestaat niet.");
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) throw new BusinessRuleException(string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    private async Task<UserModel> ToModelAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new UserModel(user.Id, user.UserName ?? "", user.Email ?? "", user.TwoFactorEnabled,
            user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow, user.OwnerId,
            (await users.GetRolesAsync(user)).OrderBy(role => role).ToList());
    }
}
