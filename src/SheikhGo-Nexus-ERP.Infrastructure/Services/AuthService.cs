using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

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
        var user = await _db.Users.IgnoreQueryFilters()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role!).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted && u.IsActive, ct);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        _tenant.Set(user.TenantId, user.Id);

        var companies = await _db.Companies
            .Where(c => c.TenantId == user.TenantId)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyBriefDto(c.Id, c.Name, c.ShortName))
            .ToListAsync(ct);

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.TenantId == user.TenantId, ct);

        var defaultBranch = company is null
            ? null
            : await _db.Branches
                .Where(b => b.CompanyId == company.Id)
                .OrderByDescending(b => b.IsDefault)
                .ThenBy(b => b.Name)
                .Select(b => new BranchDto(b.Id, b.CompanyId, b.Code, b.Name, b.IsDefault))
                .FirstOrDefaultAsync(ct);

        var currentFy = company is null
            ? null
            : await _db.FinancialYears
                .Where(y => y.CompanyId == company.Id && y.IsCurrent)
                .Select(y => new FinancialYearDto(y.Id, y.CompanyId, y.Code, y.StartDate, y.EndDate, y.IsCurrent, y.IsClosed))
                .FirstOrDefaultAsync(ct);

        var permissions = ResolvePermissions(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var role = user.UserRoles.Select(x => x.Role?.Code ?? x.Role?.Name).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "User";
        var token = CreateToken(user, role, company?.Id, defaultBranch?.Id, currentFy?.Id);
        return new LoginResponse(
            token,
            new UserDto(user.Id, user.Email, user.FullName, role, user.IsPlatformAdmin),
            company is null
                ? new CompanyDto(user.TenantId, "SheikhGo-Nexus-ERP", "NX", null, null, "Pakistan", null, null, "PKR", null, Domain.Enums.IndustryType.FeedMill)
                : new CompanyDto(company.Id, company.Name, company.ShortName, company.Address, company.City,
                    company.Country, company.Phone, company.Email, company.Currency, company.Ntn, company.Industry,
                    company.DefaultSaleTaxPercent, company.DefaultPurchaseTaxPercent),
            companies,
            defaultBranch,
            currentFy,
            permissions);
    }

    private static IReadOnlyList<string> ResolvePermissions(Domain.Entities.User user)
    {
        if (user.IsPlatformAdmin ||
            user.UserRoles.Any(r => string.Equals(r.Role?.Code, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return DbSeeder.PermissionCatalog.Select(p => p.Code).ToList();
        }

        return user.UserRoles
            .SelectMany(r => r.Role?.RolePermissions ?? Enumerable.Empty<Domain.Entities.RolePermission>())
            .Select(rp => rp.Permission?.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .OrderBy(c => c)
            .ToList();
    }

    private string CreateToken(
        Domain.Entities.User user,
        string role,
        Guid? companyId,
        Guid? branchId,
        Guid? financialYearId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role),
            new("tenant_id", user.TenantId.ToString()),
            new("is_platform_admin", user.IsPlatformAdmin ? "true" : "false")
        };
        if (companyId is Guid cid) claims.Add(new Claim("company_id", cid.ToString()));
        if (branchId is Guid bid) claims.Add(new Claim("branch_id", bid.ToString()));
        if (financialYearId is Guid fyid) claims.Add(new Claim("financial_year_id", fyid.ToString()));

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
