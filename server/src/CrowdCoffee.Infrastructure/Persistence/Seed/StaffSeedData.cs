using CrowdCoffee.Domain.Staff;

namespace CrowdCoffee.Infrastructure.Persistence.Seed;

/// <summary>
/// Dev-only staff accounts so Epic 11/12 can be exercised end-to-end without a manual admin
/// bootstrap flow (none exists yet — creating staff users is Epic 13 scope). Rotate/remove before
/// go-live, same as the Paymob "no live credentials" note in Epic 10.
///
/// The hashes below are hardcoded literals, not BCrypt.HashPassword(...) called live in this
/// getter — BCrypt salts randomly per call, so calling it here would make every future
/// "dotnet ef migrations add" see a "changed" seed value (a different valid hash for the same
/// password) and scaffold a spurious StaffUsers UpdateData migration. Computed once; verify a
/// literal still matches its password via BCrypt.Net.BCrypt.Verify(password, hash) if either ever
/// needs to change.
/// </summary>
public static class StaffSeedData
{
    public const string DevManagerUsername = "manager1";
    public const string DevManagerPassword = "Manager123!";
    private const string DevManagerPasswordHash = "$2a$11$6TAyXfMx..MMUHVpUCqr7Oey6cc4SVJ4Lga2nCotjhWdy9PJf4d0C";

    public const string DevBaristaUsername = "barista1";
    public const string DevBaristaPassword = "Barista123!";
    private const string DevBaristaPasswordHash = "$2a$11$.2ZqXoDJUnTFSypYTSAG1uM4ANZ6jozkP6XyZBBb3JH9/13jxCwoq";

    public static IReadOnlyList<StaffUser> StaffUsers =>
    [
        new StaffUser(DeterministicGuid.Create("staff:manager1"), DevManagerUsername, DevManagerPasswordHash, StaffRole.Manager),
        new StaffUser(DeterministicGuid.Create("staff:barista1"), DevBaristaUsername, DevBaristaPasswordHash, StaffRole.Barista)
    ];
}
