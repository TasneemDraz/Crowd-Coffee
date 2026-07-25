using System.Security.Cryptography;
using System.Text;

namespace CrowdCoffee.Infrastructure.Persistence.Seed;

/// <summary>
/// EF Core's HasData requires stable keys across migration regenerations — Guid.NewGuid() here
/// would make every "dotnet ef migrations add" produce a full-table diff. Hashing a stable
/// string (entity kind + slug) keeps the same row mapping to the same Guid forever.
/// </summary>
public static class DeterministicGuid
{
    public static Guid Create(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
