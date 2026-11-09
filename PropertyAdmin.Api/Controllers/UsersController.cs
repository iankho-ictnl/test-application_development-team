using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Contracts;
using PropertyAdmin.Data;

namespace PropertyAdmin.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.Users)]
public class UsersController(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<UserDto>> List(CancellationToken ct)
    {
        var list = await users.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
        var result = new List<UserDto>();
        foreach (var u in list) result.Add(await ToDto(u, ct));
        return result;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> Get(string id, CancellationToken ct)
    {
        var u = await users.FindByIdAsync(id);
        return u is null ? NotFound() : await ToDto(u, ct);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserCreateRequest r, CancellationToken ct)
    {
        var error = await ValidateRolesAndOwner(r.Roles, r.OwnerId, ct);
        if (error is not null) return Problem(error, statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        var user = new ApplicationUser { UserName = r.UserName, Email = r.Email, EmailConfirmed = true, OwnerId = r.OwnerId };
        var result = await users.CreateAsync(user, r.Password);
        if (!result.Succeeded) return Problem(string.Join(" ", result.Errors.Select(e => e.Description)), statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        await users.AddToRolesAsync(user, r.Roles.Distinct());
        return CreatedAtAction(nameof(Get), new { id = user.Id }, await ToDto(user, ct));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, UserUpdateRequest r, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var error = await ValidateRolesAndOwner(r.Roles, r.OwnerId, ct);
        if (error is not null) return Problem(error, statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        user.Email = r.Email;
        user.OwnerId = r.OwnerId;
        await users.SetLockoutEndDateAsync(user, r.LockedOut ? DateTimeOffset.MaxValue : null);
        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current.Except(r.Roles));
        await users.AddToRolesAsync(user, r.Roles.Distinct().Except(current));
        await users.UpdateSecurityStampAsync(user);
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return Problem(string.Join(" ", result.Errors.Select(e => e.Description)), statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        if (!await UserAdminRemainsAsync(ct))
            return Problem("Er moet minstens één actieve gebruiker met gebruikersbeheer overblijven.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        await tx.CommitAsync(ct);
        return await ToDto(user, ct);
    }

    [HttpPost("{id}/reset-2fa")]
    public async Task<IActionResult> ResetTwoFactor(string id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        await users.SetTwoFactorEnabledAsync(user, false);
        await users.ResetAuthenticatorKeyAsync(user);
        return NoContent();
    }

    [HttpPost("{id}/sign-out")]
    public async Task<IActionResult> ForceSignOut(string id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        await users.UpdateSecurityStampAsync(user);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await users.DeleteAsync(user);
        if (!await UserAdminRemainsAsync(ct))
            return Problem("Er moet minstens één actieve gebruiker met gebruikersbeheer overblijven.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        await tx.CommitAsync(ct);
        return NoContent();
    }

    private async Task<string?> ValidateRolesAndOwner(IReadOnlyList<string> roleNames, int? ownerId, CancellationToken ct)
    {
        foreach (var r in roleNames)
            if (!await roles.RoleExistsAsync(r)) return $"Rol '{r}' bestaat niet.";
        if (ownerId is int o && !await db.Owners.AnyAsync(x => x.Id == o)) return "Eigenaar bestaat niet.";
        return null;
    }

    private async Task<bool> UserAdminRemainsAsync(CancellationToken ct) => await UserAdminGuard.CountAsync(db, ct) > 0;

    private async Task<UserDto> ToDto(ApplicationUser u, CancellationToken ct) => new(u.Id, u.UserName ?? "", u.Email ?? "", u.TwoFactorEnabled,
        u.LockoutEnd is { } end && end > DateTimeOffset.UtcNow, u.OwnerId, (await users.GetRolesAsync(u)).OrderBy(x => x).ToList());
}

public static class UserAdminGuard
{
    /// <summary>Number of active (not locked) users that have the Users permission through a role.</summary>
    public static async Task<int> CountAsync(AppDbContext db, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var roleIds = db.RoleClaims.Where(c => c.ClaimType == Permissions.ClaimType && c.ClaimValue == Permissions.Users).Select(c => c.RoleId);
        var userIds = await db.UserRoles.Where(ur => roleIds.Contains(ur.RoleId)).Select(ur => ur.UserId).Distinct().ToListAsync(ct);
        var lockouts = await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => u.LockoutEnd).ToListAsync(ct);
        return lockouts.Count(l => l is null || l <= now);
    }
}

[ApiController]
[Route("api/roles")]
[Authorize(Policy = Permissions.Users)]
public class RolesController(RoleManager<IdentityRole> roles, AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<RoleDto>> List(CancellationToken ct)
    {
        var all = await roles.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);
        var claims = await db.RoleClaims.AsNoTracking().Where(c => c.ClaimType == Permissions.ClaimType).ToListAsync(ct);
        var counts = await db.UserRoles.AsNoTracking().GroupBy(u => u.RoleId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return all.Select(r => new RoleDto(r.Name!, claims.Where(c => c.RoleId == r.Id).Select(c => c.ClaimValue!).OrderBy(x => x).ToList(),
            counts.GetValueOrDefault(r.Id))).ToList();
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoleSaveRequest r, CancellationToken ct)
    {
        if (r.Permissions.Except(Permissions.All).Any()) return Problem("Onbekende rechten.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        if (await roles.RoleExistsAsync(r.Name)) return Problem("Rol bestaat al.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        var role = new IdentityRole(r.Name);
        await roles.CreateAsync(role);
        foreach (var p in r.Permissions.Distinct()) await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
        return NoContent();
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> Update(string name, RoleSaveRequest r, CancellationToken ct)
    {
        if (r.Permissions.Except(Permissions.All).Any()) return Problem("Onbekende rechten.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        var role = await roles.FindByNameAsync(name);
        if (role is null) return NotFound();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!string.Equals(role.Name, r.Name, StringComparison.Ordinal))
        {
            if (await roles.RoleExistsAsync(r.Name)) return Problem("Rol bestaat al.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
            await roles.SetRoleNameAsync(role, r.Name);
            await roles.UpdateAsync(role);
        }
        var current = (await roles.GetClaimsAsync(role)).Where(c => c.Type == Permissions.ClaimType).ToList();
        foreach (var c in current.Where(c => !r.Permissions.Contains(c.Value))) await roles.RemoveClaimAsync(role, c);
        foreach (var p in r.Permissions.Distinct().Where(p => current.All(c => c.Value != p)))
            await roles.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
        if (await UserAdminGuard.CountAsync(db, ct) == 0)
            return Problem("Er moet minstens één actieve gebruiker met gebruikersbeheer overblijven.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, CancellationToken ct)
    {
        var role = await roles.FindByNameAsync(name);
        if (role is null) return NotFound();
        if (await db.UserRoles.AnyAsync(u => u.RoleId == role.Id))
            return Problem("Rol is nog aan gebruikers toegekend.", statusCode: StatusCodes.Status400BadRequest, title: "Ongeldige aanvraag");
        await roles.DeleteAsync(role);
        return NoContent();
    }
}
