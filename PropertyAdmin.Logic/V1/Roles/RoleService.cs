using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Roles.Models;

namespace PropertyAdmin.Logic.V1.Roles;

// Identity's RoleManager takes no cancellation token, so each step checks it before it starts.
public sealed class RoleService(RoleManager<IdentityRole> roles, AppDbContext db) : IRoleService
{
    public const string UnknownPermissions = "Onbekende rechten.";
    public const string RoleExists = "Rol bestaat al.";
    public const string RoleInUse = "Rol is nog aan gebruikers toegekend.";
    public const string ReadOwnNotAlone = "Het recht ReadOwn kan niet met andere rechten in één rol worden gecombineerd.";

    public async Task<IReadOnlyList<RoleModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        var all = await roles.Roles.AsNoTracking().OrderBy(role => role.Name).ToListAsync(cancellationToken);
        var claims = await db.RoleClaims.AsNoTracking().Where(claim => claim.ClaimType == Permissions.ClaimType).ToListAsync(cancellationToken);
        var counts = await db.UserRoles.AsNoTracking().GroupBy(userRole => userRole.RoleId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        return all.Select(role => new RoleModel(
            role.Name!,
            claims.Where(claim => claim.RoleId == role.Id).Select(claim => claim.ClaimValue!).OrderBy(permission => permission).ToList(),
            counts.GetValueOrDefault(role.Id))).ToList();
    }

    public async Task CreateAsync(RoleSaveModel model, CancellationToken cancellationToken)
    {
        EnsureValidPermissions(model.Permissions);
        cancellationToken.ThrowIfCancellationRequested();
        if (await roles.RoleExistsAsync(model.Name)) throw new BusinessRuleException(RoleExists);
        var role = new IdentityRole(model.Name);
        cancellationToken.ThrowIfCancellationRequested();
        await roles.CreateAsync(role);
        foreach (var permission in model.Permissions.Distinct())
            await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
    }

    public async Task<bool> UpdateAsync(string name, RoleSaveModel model, CancellationToken cancellationToken)
    {
        EnsureValidPermissions(model.Permissions);
        cancellationToken.ThrowIfCancellationRequested();
        var role = await roles.FindByNameAsync(name);
        if (role is null) return false;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!string.Equals(role.Name, model.Name, StringComparison.Ordinal))
        {
            if (await roles.RoleExistsAsync(model.Name)) throw new BusinessRuleException(RoleExists);
            await roles.SetRoleNameAsync(role, model.Name);
            await roles.UpdateAsync(role);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var current = (await roles.GetClaimsAsync(role)).Where(claim => claim.Type == Permissions.ClaimType).ToList();
        foreach (var claim in current.Where(claim => !model.Permissions.Contains(claim.Value)))
            await roles.RemoveClaimAsync(role, claim);
        foreach (var permission in model.Permissions.Distinct().Where(permission => current.All(claim => claim.Value != permission)))
            await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
        await UserAdminGuard.EnsureRemainsAsync(db, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await roles.FindByNameAsync(name);
        if (role is null) return false;
        if (await db.UserRoles.AnyAsync(userRole => userRole.RoleId == role.Id, cancellationToken))
            throw new BusinessRuleException(RoleInUse);
        await roles.DeleteAsync(role);
        return true;
    }

    // S-28: ReadOwn (owner-scoped reading) may only stand alone in a role; combined with any other
    // permission it would open writes and lists that are not owner-scoped.
    private static void EnsureValidPermissions(IReadOnlyList<string> permissions)
    {
        if (permissions.Except(Permissions.All, StringComparer.Ordinal).Any()) throw new BusinessRuleException(UnknownPermissions);
        if (permissions.Contains(Permissions.ReadOwn, StringComparer.Ordinal)
            && permissions.Any(permission => !string.Equals(permission, Permissions.ReadOwn, StringComparison.Ordinal)))
            throw new BusinessRuleException(ReadOwnNotAlone);
    }
}
