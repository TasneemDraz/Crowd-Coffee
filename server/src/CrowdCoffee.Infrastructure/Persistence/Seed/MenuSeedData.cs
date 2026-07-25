using System.Reflection;
using System.Text.Json;
using CrowdCoffee.Domain.Catalog;

namespace CrowdCoffee.Infrastructure.Persistence.Seed;

/// <summary>
/// Loads menu-seed.json (extracted verbatim from the design's Menu.html per §4/§4a — not
/// retyped) and turns it into entity instances for EF Core's HasData seeding. Guids are
/// deterministic (DeterministicGuid) so migration regeneration doesn't churn every row.
///
/// Recipe rows are deliberately NOT seeded here — ingredient quantities per drink (grams of
/// beans, ml of milk) aren't in the design files and are TBD business data (see Story 2.5 notes
/// in docs/EPIC_BREAKDOWN.md). Seeding stops at the catalog/modifier structure.
/// </summary>
public static class MenuSeedData
{
    private static readonly Lazy<MenuSeedJson> Json = new(LoadJson);

    private const string MilkGroupSlug = "milk";
    private const string ShotGroupSlug = "shot";
    private const string SyrupsGroupSlug = "syrups";

    public static IReadOnlyList<Category> Categories => Json.Value.Categories
        .Select((c, index) => new Category(
            DeterministicGuid.Create($"category:{c.Id}"),
            c.Id,
            c.Label,
            Json.Value.Descriptions.GetValueOrDefault(c.Id),
            index))
        .ToList();

    public static IReadOnlyList<Product> Products
    {
        get
        {
            var perCategoryIndex = new Dictionary<string, int>();
            return Json.Value.Items.Select(item =>
            {
                var displayOrder = perCategoryIndex.GetValueOrDefault(item.Cat, 0);
                perCategoryIndex[item.Cat] = displayOrder + 1;

                return new Product(
                    DeterministicGuid.Create($"product:{item.Id}"),
                    item.Cat,
                    item.Id,
                    item.Name,
                    item.Desc,
                    item.Price,
                    displayOrder);
            }).ToList();
        }
    }

    public static IReadOnlyList<ModifierGroup> ModifierGroups =>
    [
        new(DeterministicGuid.Create($"modifiergroup:{MilkGroupSlug}"), MilkGroupSlug, "Milk Type", ModifierSelectionType.ExactlyOne, 1, true, 0),
        new(DeterministicGuid.Create($"modifiergroup:{ShotGroupSlug}"), ShotGroupSlug, "Espresso Shot", ModifierSelectionType.UpToN, 1, false, 1),
        new(DeterministicGuid.Create($"modifiergroup:{SyrupsGroupSlug}"), SyrupsGroupSlug, "Syrups", ModifierSelectionType.UpToN, 2, false, 2)
    ];

    public static IReadOnlyList<ModifierOption> ModifierOptions
    {
        get
        {
            var milkGroupId = DeterministicGuid.Create($"modifiergroup:{MilkGroupSlug}");
            var shotGroupId = DeterministicGuid.Create($"modifiergroup:{ShotGroupSlug}");
            var syrupsGroupId = DeterministicGuid.Create($"modifiergroup:{SyrupsGroupSlug}");

            var options = new List<ModifierOption>();

            options.AddRange(Json.Value.MilkOptions.Select((m, index) => new ModifierOption(
                DeterministicGuid.Create($"modifieroption:{MilkGroupSlug}:{m.Id}"),
                milkGroupId, m.Id, m.Name, m.Price, index)));

            options.Add(new ModifierOption(
                DeterministicGuid.Create($"modifieroption:{ShotGroupSlug}:{Json.Value.ShotOption.Id}"),
                shotGroupId, Json.Value.ShotOption.Id, Json.Value.ShotOption.Name, Json.Value.ShotOption.Price, 0));

            options.AddRange(Json.Value.SyrupOptions.Select((s, index) => new ModifierOption(
                DeterministicGuid.Create($"modifieroption:{SyrupsGroupSlug}:{s.Id}"),
                syrupsGroupId, s.Id, s.Name, s.Price, index)));

            return options;
        }
    }

    /// Category-wide mapping per §4a's DRINK_CATS rule: every drink category gets all three groups.
    public static IReadOnlyList<ProductModifierGroup> ProductModifierGroups
    {
        get
        {
            var groupIds = new[]
            {
                DeterministicGuid.Create($"modifiergroup:{MilkGroupSlug}"),
                DeterministicGuid.Create($"modifiergroup:{ShotGroupSlug}"),
                DeterministicGuid.Create($"modifiergroup:{SyrupsGroupSlug}")
            };

            return Json.Value.DrinkCategories
                .SelectMany(categorySlug => groupIds.Select(groupId =>
                    new ProductModifierGroup(
                        DeterministicGuid.Create($"productmodifiergroup:{categorySlug}:{groupId}"),
                        categorySlug,
                        groupId)))
                .ToList();
        }
    }

    private static MenuSeedJson LoadJson()
    {
        var assembly = typeof(MenuSeedData).Assembly;
        const string resourceName = "CrowdCoffee.Infrastructure.Persistence.Seed.menu-seed.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        var seed = JsonSerializer.Deserialize<MenuSeedJson>(stream)
            ?? throw new InvalidOperationException("Failed to deserialize menu-seed.json.");

        return seed;
    }
}
