using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;

namespace PropertyAdmin.Logic;

/// <summary>Keeps at least one active user with user management (Users and Roles features, D-035).</summary>
internal static class UserAdminGuard
{
    public const string Message = "Er moet minstens één actieve gebruiker met gebruikersbeheer overblijven.";

    /// <summary>Number of active (not locked) users that have the Users permission through a role.</summary>
    public static async Task<int> CountAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var roleIds = db.RoleClaims.Where(c => c.ClaimType == Permissions.ClaimType && c.ClaimValue == Permissions.Users).Select(c => c.RoleId);
        var userIds = await db.UserRoles.Where(ur => roleIds.Contains(ur.RoleId)).Select(ur => ur.UserId).Distinct().ToListAsync(cancellationToken);
        var lockouts = await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => u.LockoutEnd).ToListAsync(cancellationToken);
        return lockouts.Count(l => l is null || l <= now);
    }

    public static async Task EnsureRemainsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await CountAsync(db, cancellationToken) == 0) throw new BusinessRuleException(Message);
    }
}
