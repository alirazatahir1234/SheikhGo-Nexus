using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SoftaxeERP.Application.DTOs;
using SoftaxeERP.Application.Interfaces;
using SoftaxeERP.Infrastructure.Persistence;

namespace SoftaxeERP.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ITenantContext _tenant;

    public AuthService(AppDbContext db, IConfiguration config, ITenantContext tenant)
    {
        _db = db;
        _config = config;
        _tenant = tenant;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.IgnoreQueryFilters().Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted && u.IsActive, ct);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        _tenant.Set(user.TenantId, user.Id);

        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.TenantId == user.TenantId, ct);

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var role = user.UserRoles.Select(x => x.Role?.Code ?? x.Role?.Name).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "User";
        var token = CreateToken(user, role);
        return new LoginResponse(
            token,
            new UserDto(user.Id, user.Email, user.FullName, role, user.IsPlatformAdmin),
            company is null
                ? new CompanyDto(user.TenantId, "Softaxe ERP", "SX", null, null, "Pakistan", null, null, "PKR", null, Domain.Enums.IndustryType.FeedMill)
                : new CompanyDto(company.Id, company.Name, company.ShortName, company.Address, company.City,
                    company.Country, company.Phone, company.Email, company.Currency, company.Ntn, company.Industry));
    }

    private string CreateToken(Domain.Entities.User user, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim("is_platform_admin", user.IsPlatformAdmin ? "true" : "false")
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
