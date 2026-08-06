using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Infrastructure.Persistence.Seed;

/// <summary>
/// Dev-only staff accounts so Epic 11/12 can be exercised end-to-end without a manual admin
/// bootstrap flow (none exists yet — creating staff users is Epic 13 scope). BCrypt.HashPassword
/// runs once, at "dotnet ef migrations add" time, and only the resulting hash is baked into the
/// generated migration as a literal — the plaintext passwords below never reach the database.
/// Rotate/remove before go-live, same as the Paymob "no live credentials" note in Epic 10.
/// </summary>
public static class StaffSeedData
{
    public const string DevManagerUsername = "manager1";
    public const string DevManagerPassword = "Manager123!";

    public const string DevBaristaUsername = "barista1";
    public const string DevBaristaPassword = "Barista123!";

    public static IReadOnlyList<StaffUser> StaffUsers =>
    [
        new StaffUser(
            DeterministicGuid.Create("staff:manager1"),
            DevManagerUsername,
            BCrypt.Net.BCrypt.HashPassword(DevManagerPassword),
            StaffRole.Manager),
        new StaffUser(
            DeterministicGuid.Create("staff:barista1"),
            DevBaristaUsername,
            BCrypt.Net.BCrypt.HashPassword(DevBaristaPassword),
            StaffRole.Barista)
    ];
}
