using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SoftaxeERP.Infrastructure.Persistence;

/// <summary>
/// Multi-layer guard: Softaxe live deny-list + Development allowlist for SheikhGo_Nexus_Dev.
/// SQL permissions on Softaxe live remain the strongest layer (document in README).
/// </summary>
public static class DatabaseSafetyGuard
{
    private static readonly string[] SoftaxeLiveMarkers =
    [
        "PremierFoodTest",
        "Softaxe_Live",
        "SoftaxeErpPos",
        "cc210d8fee9e.sn.mynetname.net"
    ];

    public const string AllowedDevDatabase = "SheikhGo_Nexus_Dev";

    public static void EnsureSafe(IConfiguration config, IHostEnvironment env)
    {
        var cs = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing.");

        foreach (var marker in SoftaxeLiveMarkers)
        {
            if (cs.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Live Softaxe database is prohibited. Connection string contains '{marker}'. " +
                    $"Use {AllowedDevDatabase} only.");
            }
        }

        var builder = new SqlConnectionStringBuilder(cs);
        var dbName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(dbName))
            throw new InvalidOperationException("DefaultConnection must specify a Database/Initial Catalog.");

        if (env.IsDevelopment() &&
            !string.Equals(dbName, AllowedDevDatabase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Development application may only connect to {AllowedDevDatabase}. Got '{dbName}'.");
        }
    }
}
