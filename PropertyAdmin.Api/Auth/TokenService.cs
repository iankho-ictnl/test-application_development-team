using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropertyAdmin.Logic.V1.Auth;
using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Api.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "PropertyAdmin.Api";
    public string Audience { get; set; } = "PropertyAdmin";
    public string Key { get; set; } = "";
    public int ExpiryHours { get; set; } = 8;
    public int ChallengeMinutes { get; set; } = 5;
}

public static class AppClaims
{
    public const string OwnerId = "owner_id";
    public const string SecurityStamp = "sstamp";
    public const string Purpose = "purpose";
    public const string AccessPurpose = "access";
    public const string TwoFactorPurpose = "2fa";
}

public class TokenService(IOptions<JwtOptions> options, IMemoryCache cache) : ITokenIssuer
{
    private readonly JwtOptions _o = options.Value;

    public static SymmetricSecurityKey CreateKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        if (bytes.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 256 bits (32 bytes).");
        return new SymmetricSecurityKey(bytes);
    }

    public AccessTokenModel CreateAccessToken(TokenSubjectModel subject)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.UserId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, subject.UserName),
            new(AppClaims.SecurityStamp, subject.SecurityStamp),
            new(AppClaims.Purpose, AppClaims.AccessPurpose),
        };
        if (subject.OwnerId is int ownerId)
            claims.Add(new Claim(AppClaims.OwnerId, ownerId.ToString()));
        claims.AddRange(subject.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(subject.Permissions.Select(p => new Claim(Permissions.ClaimType, p)));

        var expires = DateTime.UtcNow.AddHours(_o.ExpiryHours);
        return new AccessTokenModel(Write(claims, expires, _o.Audience), expires);
    }

    public string CreateChallengeToken(string userId, string securityStamp)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaims.SecurityStamp, securityStamp),
            new(AppClaims.Purpose, AppClaims.TwoFactorPurpose),
        };
        return Write(claims, DateTime.UtcNow.AddMinutes(_o.ChallengeMinutes), _o.Audience + ".2fa");
    }

    /// <summary>Validates a 2FA challenge token once; returns the user id or null.</summary>
    public string? ConsumeChallengeToken(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = _o.Issuer,
                ValidAudience = _o.Audience + ".2fa",
                IssuerSigningKey = CreateKey(_o.Key),
                ClockSkew = TimeSpan.FromSeconds(30),
            }, out var validated);

            if (principal.FindFirstValue(AppClaims.Purpose) != AppClaims.TwoFactorPurpose) return null;
            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (jti is null) return null;
            var cacheKey = "2fa-used:" + jti;
            if (cache.TryGetValue(cacheKey, out _)) return null;
            cache.Set(cacheKey, true, validated.ValidTo.AddMinutes(1) - DateTime.UtcNow);
            return principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private string Write(IEnumerable<Claim> claims, DateTime expires, string audience)
    {
        var creds = new SigningCredentials(CreateKey(_o.Key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_o.Issuer, audience, claims, DateTime.UtcNow, expires, creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
